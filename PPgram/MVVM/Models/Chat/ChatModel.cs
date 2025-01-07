using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace PPgram.MVVM.Models.Chat;
/// <summary>
/// A base class that represents a PPgram chat with its functionality
/// </summary>
internal abstract partial class ChatModel : ObservableObject
{
    [ObservableProperty]
    private ProfileModel profile = new();
    [ObservableProperty]
    private ChatItem? selectedMessage;
    [ObservableProperty]
    private string lastMessage = string.Empty;
    [ObservableProperty]
    private long lastMessageTime;
    [ObservableProperty]
    private MessageStatus lastMessageStatus;
    [ObservableProperty]
    private ObservableCollection<ChatItem> messages = [];
    [ObservableProperty]
    private ObservableCollection<FileModel> files = [];

    [ObservableProperty]
    private string messageInput = string.Empty;
    [ObservableProperty]
    private string secondaryText = string.Empty;
    [ObservableProperty]
    private string secondaryHeader = string.Empty;
    [ObservableProperty]
    private bool secondaryVisible;

    [ObservableProperty]
    private bool inReply;
    [ObservableProperty]
    private bool inEdit;

    [ObservableProperty]
    private bool canReply;
    [ObservableProperty]
    private bool canEdit;
    [ObservableProperty]
    private bool canDelete;
    [ObservableProperty]
    private bool contextVisible;

    [ObservableProperty]
    private int unreadCount;
    [ObservableProperty]
    private ChatStatus status;

    public int Id { get; set; }
    public bool Searched = false;

    private readonly MessageChainManager chainManager = new();
    private readonly ReplyModel reply = new();
    private readonly DispatcherTimer timer;
    protected readonly ProfileState profileState = ProfileState.Instance;
    public ChatModel()
    {
        // draft update request delay timer
        timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += SendDraft;
    }
    protected abstract void UpdateLastMessage();
    protected abstract void UpdateStatus();
    partial void OnStatusChanged(ChatStatus value) => UpdateStatus();
    partial void OnSelectedMessageChanged(ChatItem? value)
    {
        if (value is MessageModel message)
        {
            ContextVisible = true;
            CanEdit = message.SenderId == profileState.UserId && message.Content is ITextContent;
        }
        else
        {
            ContextVisible = false;
            Dispatcher.UIThread.Post(() => SelectedMessage = null);
        }
    }
    partial void OnMessageInputChanged(string value)
    {
        timer.Stop();
        if (!String.IsNullOrEmpty(value.Trim()) && value.Length <= 2500)
        {
            // restart delay if draft is valid
            timer.Start();
        }
    }
    public void AttachFiles(List<FileModel> files)
    {
        foreach (FileModel file in files)
        {
            Files.Add(file);
        }
    }
    public void LoadMessages(List<MessageModel> messageList)
    {
        Messages = chainManager.GenerateChain(messageList, this);
        UpdateLastMessage();
    }
    public void AddMessage(MessageModel message)
    {
        chainManager.AddChain(message, Messages, this);
        UpdateLastMessage();
    }
    public void EditMessage(MessageModel message)
    {
        if (TryFindMessage(message.Id, out var origin))
        {
            origin = message;
            origin.Edited = true;
            UpdateLastMessage();
        }
    }
    public void DeleteMessage(int id)
    {
        if (TryFindMessage(id, out var message))
        {
            chainManager.DeleteChain(message, Messages, this);
            UpdateLastMessage();
        }
    }
    public void ChangeMessageStatus(int id, MessageStatus status)
    {
        if (TryFindMessage(id, out var message))
        {
            message.Status = status;
            UpdateLastMessage();
        }
    }
    private void SendMessage(MessageModel message)
    {
        WeakReferenceMessenger.Default.Send(new Msg_SendMessage()
        {
            message = message,
            to = this
        });
    }
    private void SendEditMessage(MessageModel message, MessageContentModel editedContent)
    {
        WeakReferenceMessenger.Default.Send(new Msg_EditMessage
        {
            chat = message.Chat,
            Id = message.Id,
            newContent = editedContent
        });
    }
    private void SendDraft(object? sender, EventArgs e)
    {
        timer.Stop();
        if (!Searched) WeakReferenceMessenger.Default.Send(new Msg_SendDraft { draft = MessageInput.Trim(), chat_id = Id });
    }
    private bool TryFindMessage(int id, out MessageModel message)
    {
        MessageModel? msg_or_null = Messages.OfType<MessageModel>().FirstOrDefault(m => m.Id == id);
        message = msg_or_null ?? default!;
        return msg_or_null != null;
    }
    [RelayCommand]
    private void BuildMessage()
    {
        // send edit request if editing
        if (InEdit && SelectedMessage is MessageModel editMessage && editMessage.Content is ITextContent tc)
        {
            if (tc.Text == MessageInput.Trim())
            {
                CloseSecondary();
                return;
            }
            tc.Text = MessageInput.Trim();
            editMessage.Edited = true;
            CloseSecondary();
            SendEditMessage(editMessage, editMessage.Content);
            return;
        }
        // create message
        MessageModel message = new()
        {
            Chat = Id,
            ReplyTo = null,
            Role = MessageRole.OwnFirst,
            Color = profileState.Color,
            SenderId = profileState.UserId,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Status = MessageStatus.Sending
        };
        // add reply if set
        if (InReply && SelectedMessage is MessageModel selectedMessage)
        {
            message.ReplyTo = selectedMessage.Id;
            message.Reply = reply;
        }
        // add content & send
        if (Files.Count != 0)
        {
            FileContentModel content = new() { Files = new(Files), Text = MessageInput.Trim() };
            message.Content = content;
            Files.Clear();
            SendMessage(message);
        }
        else if (!String.IsNullOrEmpty(MessageInput))
        {
            message.Content = new TextContentModel { Text = MessageInput.Trim() };
            SendMessage(message);
        }
        else return;
        CloseSecondary();
        chainManager.AddChain(message, Messages, this);
        UpdateLastMessage();
        MessageInput = "";
    }
    [RelayCommand]
    private void AddReply()
    {
        if (SelectedMessage != null && SelectedMessage is MessageModel message)
        {
            SecondaryVisible = true;
            InEdit = false;
            InReply = true;
            if (message.SenderId == profileState.UserId) reply.Name = profileState.Name;
            else reply.Name = Profile?.Name ?? string.Empty;

            if (message.Content is TextContentModel textcontent) reply.Text = textcontent.Text;

            SecondaryHeader = $"Reply to {reply.Name}";
            SecondaryText = reply.Text;
        }
    }
    [RelayCommand]
    private void OpenAttachFiles()
    {
        WeakReferenceMessenger.Default.Send(new Msg_OpenAttachFiles());
    }
    [RelayCommand]
    private void RemoveFile(FileModel file)
    {
        Files.Remove(file);
    }
    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
    }
    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedMessage != null && SelectedMessage is MessageModel message)
        {
            SecondaryVisible = true;
            InReply = false;
            InEdit = true;
            SecondaryHeader = "Editing message";
            if (message.Content is ITextContent tc) MessageInput = SecondaryText = tc.Text;
        }
    }
    [RelayCommand]
    private void DeleteMessage()
    {
        if (SelectedMessage != null && SelectedMessage is MessageModel message)
        {
            chainManager.DeleteChain(message, Messages, this);
            UpdateLastMessage();
            WeakReferenceMessenger.Default.Send(new Msg_DeleteMessage
            {
                chat = message.Chat,
                Id = message.Id
            });
        }
    }
    [RelayCommand]
    private void CloseSecondary()
    {
        if (InEdit) MessageInput = "";
        InEdit = false;
        InReply = false;
        SecondaryVisible = false;
        SelectedMessage = null;
    }
}
