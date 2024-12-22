using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Chat;
/// <summary>
/// A base class that represents a PPgram chat with its functionality
/// </summary>
internal abstract partial class ChatModel : ObservableObject
{
    [ObservableProperty]
    private ProfileModel? profile;
    [ObservableProperty]
    private ChatItem? selectedMessage;
    [ObservableProperty]
    private string lastMessage = "";
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
    [ObservableProperty]
    private long date;

    public int Id { get; set; }

    private ConcurrentQueue<MessageModel> sendingQueue = [];
    private readonly MessageChainManager chainManager = new();
    private readonly ReplyModel reply = new();
    protected readonly ProfileState profileState = ProfileState.Instance;

    public ChatModel()
    {
        Messages.CollectionChanged += UpdateLastMessage;
    }
    private void UpdateLastMessage() => UpdateLastMessage(this, new(NotifyCollectionChangedAction.Reset));
    protected abstract void UpdateLastMessage(object? sender, NotifyCollectionChangedEventArgs e);
    public void LoadMessages(List<MessageModel> messageList)
    {
        Messages = chainManager.GenerateChain(messageList, this);
    }
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
    private void SendMessage(MessageModel message)
    {
        // send message
        WeakReferenceMessenger.Default.Send(new Msg_SendMessage()
        {
            message = message,
            to = Id
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
    public void AttachFiles(List<FileModel> files)
    {
        if (Files.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                dialog = new AttachFileDialog { Files = Files, canSkip = false }
            });
        }
        foreach (FileModel file in files)
        {
            Files.Add(file);
        }
    }
    public void AddMessage(MessageModel message)
    {
        Messages.Add(message);
        chainManager.AddChain(Messages);
    }
    public void EditMessage(MessageModel message)
    {
        MessageModel? origin = Messages.OfType<MessageModel>().FirstOrDefault(m => m.Id == message.Id);
        if (origin == null) return;
        origin = message;
        origin.Edited = true;
    }
    public void DeleteMessage(int id)
    {
        MessageModel? origin = Messages.OfType<MessageModel>().FirstOrDefault(m => m.Id == id);
        if (origin == null) return;
        chainManager.DeleteChain(origin, Messages);
    }
    public void ChangeMessageStatus(int id, MessageStatus status)
    {
        
    }
    [RelayCommand]
    private void BuildMessage()
    {
        
        // send edit request if editing
        if (InEdit && SelectedMessage is MessageModel editMessage)
        {
            if (editMessage.Content is ITextContent tc)
            {
                if (tc.Text == MessageInput.Trim())
                {
                    CloseSecondary();
                    return;
                }
                tc.Text = MessageInput.Trim();
            }
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
            FileContentModel content = new FileContentModel { Files = new(Files), Text = MessageInput.Trim() };
            message.Content = content;
            Files.Clear();
            WeakReferenceMessenger.Default.Send(new Msg_UploadFiles { files = content.Files });
            WeakReferenceMessenger.Default.Register<Msg_UploadFilesResult>(this, (r, e) =>
            {
                WeakReferenceMessenger.Default.Unregister<Msg_UploadFilesResult>(this);
                if (e.ok) SendMessage(message);
                else message.Status = MessageStatus.Error;
            });
        }
        else if (!String.IsNullOrEmpty(MessageInput))
        {
            message.Content = new TextContentModel { Text = MessageInput.Trim() };
            CloseSecondary();
            SendMessage(message);
        }
        else return;
        Messages.Add(message);
        chainManager.AddChain(Messages);
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
            chainManager.DeleteChain(message, Messages);
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
