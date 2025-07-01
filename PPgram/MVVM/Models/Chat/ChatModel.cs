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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;

namespace PPgram.MVVM.Models.Chat;
/// <summary>
/// A base class that represents a PPgram chat with its functionality
/// </summary>
internal abstract partial class ChatModel : ObservableObject
{
    [ObservableProperty]
    public partial ProfileModel Profile { get; set; } = new();
    [ObservableProperty]
    public partial ChatItem? SelectedMessage { get; set; }
    [ObservableProperty]
    public partial string LastMessage { get; set; } = string.Empty;
    [ObservableProperty]
    public partial long LastMessageTime { get; set; }
    [ObservableProperty]
    public partial MessageStatus LastMessageStatus { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<ChatItem> Messages { get; set; } = [];
    [ObservableProperty]
    public partial ObservableCollection<FileModel> Files { get; set; } = [];
    [ObservableProperty]
    public partial string MessageInput { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SecondaryText { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SecondaryHeader { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool SecondaryVisible { get; set; }
    [ObservableProperty]
    public partial bool ToEndVisible { get; set; }
    [ObservableProperty]
    public partial bool InReply { get; set; }
    [ObservableProperty]
    public partial bool InEdit { get; set; }
    [ObservableProperty]
    public partial bool CanReply { get; set; }
    [ObservableProperty]
    public partial bool CanEdit { get; set; }
    [ObservableProperty]
    public partial bool CanDelete { get; set; }
    [ObservableProperty]
    public partial bool ContextVisible { get; set; }
    [ObservableProperty]
    public partial int UnreadCount { get; set; }
    [ObservableProperty]
    public partial ChatStatus Status { get; set; }
    [ObservableProperty]
    public partial bool Fetching { get; set; }

    public int Id { get; set; } = -1;
    public bool Searched { get; set; }
    public bool FetchedAllMessages { get; set; }
    private bool draftThrottle;
    private readonly ReplyModel reply = new();
    private readonly DispatcherTimer draft_timer;
    private readonly DispatcherTimer unload_timer;
    protected readonly ProfileState profileState = ProfileState.Instance;
    protected readonly AppState appState = AppState.Instance;
    public ChatModel()
    {
        // unload timer
        unload_timer = new() { Interval = TimeSpan.FromSeconds(appState.MessagesUnloadTime) };
        unload_timer.Tick += (s, e) => UnloadMessages();
        // draft request delay timer
        draft_timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
        draft_timer.Tick += (s, e) => { draftThrottle = false;};
        draft_timer.Start();
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
        if (!Searched && !String.IsNullOrEmpty(value.Trim()) && value.Length <= 2500 && !draftThrottle)
        {
            WeakReferenceMessenger.Default.Send(new Msg_SendDraft { draft = MessageInput.Trim(), chat_id = Id });
            draftThrottle = true;
        }
    }
    public void AttachFiles(List<FileModel> files)
    {
        foreach (FileModel file in files)
        {
            Files.Add(file);
        }
    }
    public void LoadMessages(List<MessageModel> messages, bool forward)
    {
        Fetching = false;
        MessageChainer.AddChain(messages, Messages, this, forward);
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
            MessageChainer.DeleteChain(message, Messages, this);
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
    public void StartUnloadTimer() => unload_timer.Start();
    public void StopUnloadTimer() => unload_timer.Stop();
    private void UnloadMessages()
    {
        MessageChainer.UnloadChain(Messages, this);
        FetchedAllMessages = false;
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
    private bool TryFindMessage(int id, out MessageModel message)
    {
        MessageModel? msg_or_null = Messages.OfType<MessageModel>().Where(m => m.Id == id).First();
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
        MessageChainer.AddChain([message], Messages, this, true);
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
            reply.Sender = message.Sender;
            if (message.Content is TextContentModel tc) reply.Text = tc.Text;
            else if (message.Content is FileContentModel fc) reply.Text = $"{fc.Files.Count} File(s)";
            SecondaryHeader = $"Reply to {reply.Sender.Name}";
            SecondaryText = reply.Text;
        }
    }
    [RelayCommand]
    private void OpenAttachFiles()
    {
        WeakReferenceMessenger.Default.Send(new Msg_OpenAttachFiles());
    }
    [RelayCommand]
    private void ForwardMessage()
    {
        if (SelectedMessage != null && SelectedMessage is MessageModel message)
        {
            WeakReferenceMessenger.Default.Send(new Msg_OpenForward() { message = message});
        }
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
            MessageChainer.DeleteChain(message, Messages, this);
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
