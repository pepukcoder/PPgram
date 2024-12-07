using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Media;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ChatItem> messageList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> chatList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> searchList = [];
    [ObservableProperty]
    private ObservableCollection<FileModel> files = [];

    [ObservableProperty]
    private ChatItem? messageListSelected = null;
    [ObservableProperty]
    private ChatModel? chatListSelected = new UserModel();
    [ObservableProperty]
    private ChatModel? searchListSelected = new UserModel();

    [ObservableProperty]
    private ProfileModel currentProfile = new();
    [ObservableProperty]
    private string chatStatus = "last seen 12:34";
    [ObservableProperty]
    private ProfileState profileState = ProfileState.Instance;

    [ObservableProperty]
    private string messageInput = string.Empty;
    [ObservableProperty]
    private string searchInput = string.Empty;

    [ObservableProperty]
    private bool inReply;
    [ObservableProperty]
    private bool inEdit;
    [ObservableProperty]
    private string secondaryInputHeader = string.Empty;
    [ObservableProperty]
    private string secondaryInputText = string.Empty;

    [ObservableProperty]
    private bool secondaryInputVisible;
    [ObservableProperty]
    private bool rightGridVisible;

    [ObservableProperty]
    private bool contextVisible;
    [ObservableProperty]
    private bool canReply;
    [ObservableProperty]
    private bool canEdit;
    [ObservableProperty]
    private bool canDelete;

    [ObservableProperty]
    private MediaPreviewer mediaPreviewer = new();

    private readonly DispatcherTimer _timer;
    private readonly MessageChainManager chainManager = new();
    private readonly ReplyModel reply = new();
    private bool inSearch;
    
    public ChatViewModel()
    {
        RightGridVisible = false;
        MediaPreviewer.Visible = true;
        MediaPreviewer.VideoControlVisible = true;
        // search request delay timer
        _timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
        _timer.Tick += SearchChat;

        WeakReferenceMessenger.Default.Register<Msg_ChangeMessageStatus>(this, (r, e) => ChangeMessageStatus(e.chat, e.Id, e.status));
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessageEvent>(this, (r, e) =>
        {
            ObservableCollection<ChatItem>? messages = ChatList.FirstOrDefault(c => c.Id == e.chat)?.Messages;
            if (messages == null) return;
            MessageModel? originalMessage = messages.OfType<MessageModel>().FirstOrDefault(m => m.Id == e.Id);
            if (originalMessage == null) return;
            chainManager.DeleteChain(originalMessage, messages);
        });
        WeakReferenceMessenger.Default.Register<Msg_SendAttachFiles>(this, (r, e) => 
        {
            MessageInput = e.description;
            BuildMessage();
        });
    }
    partial void OnMessageListSelectedChanged(ChatItem? value)
    {
        if (value is MessageModel message)
        {
            ContextVisible = true;
            CanEdit = message.SenderId == ProfileState.UserId;
        }
        else
        {
            ContextVisible = false;
            Dispatcher.UIThread.Post(() => MessageListSelected = null);
        }
    }
    partial void OnSearchListSelectedChanged(ChatModel? value)
    {
        if (String.IsNullOrEmpty(SearchInput)) return;
        if (ChatList.Any(c => c.Id == value?.Id))
        {
            inSearch = false;
            ChatListSelected = ChatList.FirstOrDefault(c => c.Id == value?.Id) ?? ChatList[0];
            ClearSearch();
        }
        else
        {
            CurrentProfile = value?.Profile ?? CurrentProfile;
            MessageList = [];
        }
    }
    partial void OnChatListSelectedChanged(ChatModel? value)
    {
        CurrentProfile = value?.Profile ?? CurrentProfile;
        if (value?.Messages.Count == 0 && !inSearch)
        {
            WeakReferenceMessenger.Default.Send(new Msg_FetchMessages()
            {
                chatId = value.Id,
            });
        }
        MessageList = value?.Messages ?? [];
    }
    partial void OnSearchInputChanged(string value)
    {
        // stop timer when editing search query
        _timer.Stop();
        // restart delay if username is not null
        // clean searchlist if search closed
        if (!String.IsNullOrEmpty(value.Trim()))
        {
            _timer.Start();
            inSearch = true;
            MessageList = [];
        }
        else
        {
            MessageList = ChatListSelected?.Messages ?? [];
            inSearch = false;
        }
    }
    private void SearchChat(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        _timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_SearchChats { searchQuery = SearchInput.Trim() });
    }
    private void SendMessage(MessageModel message)
    {
        // check if selected sender is valid
        if (ChatListSelected?.Id == 0 || ChatListSelected == null) return;
        // send message
        WeakReferenceMessenger.Default.Send(new Msg_SendMessage()
        {
            message = message,
            to = ChatListSelected.Id
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
        CloseSecondaryInput();
    }
    private void OpenInPreviewer()
    {

    }
    public void UpdateSearch(ObservableCollection<ChatModel> resultList)
    {
        SearchList = resultList;
        SearchListSelected = null;
    }
    public void UpdateChats(ObservableCollection<ChatModel> chatList) => ChatList = chatList;
    public void UpdateMessages(ObservableCollection<MessageModel> messageList)
    {
        if (ChatListSelected == null) return;
        MessageList = ChatListSelected.Messages = chainManager.GenerateChain(messageList, ChatListSelected);
    }
    public void AddChat(ChatModel chat) => ChatList.Add(chat);
    public void AddMessage(MessageModel message)
    {
        ObservableCollection<ChatItem>? messages = ChatList.FirstOrDefault(c => c.Id == message.Chat)?.Messages;
        if (messages == null) return;
        messages.Add(message);
        chainManager.AddChain(messages);
    }
    public void ChangeMessageStatus(int chat, int id, MessageStatus status)
    {
        MessageModel? message = ChatList.FirstOrDefault(c => c.Id == chat)?.Messages.OfType<MessageModel>().LastOrDefault();
        if (message != null)
        {
            message.Id = id;
            message.Status = status;
        }
    }
    public void EditMessage(MessageModel newMessage)
    {
        ObservableCollection<ChatItem>? messages = ChatList.FirstOrDefault(c => c.Id == newMessage.Chat)?.Messages;
        if (messages == null) return;
        MessageModel? originalMessage = messages.OfType<MessageModel>().FirstOrDefault(m => m.Id == newMessage.Id);
        if (originalMessage == null) return;
        originalMessage.Edited = true;
        originalMessage.Content = newMessage.Content;
    }
    public void AttachFiles(List<FileModel> files)
    {
        if (ChatListSelected?.Id == 0 || ChatListSelected == null) return;
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
    [RelayCommand]
    private void BuildMessage()
    {
        // move chat from search to chats
        if (inSearch && SearchListSelected != null)
        {
            ChatList.Add(SearchListSelected);
            ChatListSelected = SearchListSelected;
            ClearSearch();
            inSearch = false;
        }
        // check if selected sender is valid
        if (ChatListSelected?.Id == 0 || ChatListSelected == null) return;

        // send edit request if editing
        if (InEdit && MessageListSelected is MessageModel editMessage)
        {
            if (editMessage.Content is ITextContent tc)
            {
                if (tc.Text == MessageInput.Trim())
                {
                    CloseSecondaryInput();
                    return;
                }
                tc.Text = MessageInput.Trim();
            }
            editMessage.Edited = true;
            SendEditMessage(editMessage, editMessage.Content);
            return;
        }

        // create message
        MessageModel message = new()
        {
            Chat = ChatListSelected.Id,
            ReplyTo = null,
            Role = MessageRole.OwnFirst,
            Color = ProfileState.Color,
            SenderId = ProfileState.UserId,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Status = MessageStatus.Sending
        };
        // add reply if set
        if (InReply && MessageListSelected is MessageModel selectedMessage)
        {
            message.ReplyTo = selectedMessage.Id;
            message.Reply = reply;
            CloseSecondaryInput();
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
            SendMessage(message);
        }
        else return;
        // add message to ui
        ObservableCollection<ChatItem>? messages = ChatList.FirstOrDefault(c => c.Id == ChatListSelected.Id)?.Messages;
        if (messages == null) return;
        messages.Add(message);
        chainManager.AddChain(messages);
        MessageInput = "";
    }
    [RelayCommand]
    private void DeleteMessage()
    {
        if (MessageListSelected != null && MessageListSelected is MessageModel message)
        {
            chainManager.DeleteChain(message, MessageList);
            WeakReferenceMessenger.Default.Send(new Msg_DeleteMessage
            {
                chat = message.Chat,
                Id = message.Id
            });
        }    
    }
    [RelayCommand]
    private void StartEdit()
    {
        if (MessageListSelected != null && MessageListSelected is MessageModel message)
        {
            SecondaryInputVisible = true;
            InReply = false;
            InEdit = true;
            SecondaryInputHeader = "Editing message";
            if (message.Content is ITextContent tc) MessageInput = SecondaryInputText = tc.Text;
        }
    }
    [RelayCommand]
    private void AddReply()
    {
        if (MessageListSelected != null && MessageListSelected is MessageModel message)
        {
            SecondaryInputVisible = true;
            InEdit = false;
            InReply = true;
            if (message.SenderId == ProfileState.UserId) reply.Name = ProfileState.Name;
            else reply.Name = ChatListSelected?.Profile.Name ?? string.Empty;
            
            if (message.Content is TextContentModel textcontent) reply.Text = textcontent.Text;

            SecondaryInputHeader = $"Reply to {reply.Name}";
            SecondaryInputText = reply.Text;
        }
    }
    [RelayCommand]
    private void CloseSecondaryInput()
    {
        if (InEdit)
        {
            MessageInput = "";
            InEdit = false;
        }
        InReply = false;
        SecondaryInputVisible = false;
        MessageListSelected = null;
    }
    [RelayCommand]
    private void ClearSearch()
    {
        SearchInput = string.Empty;
        inSearch = false;
    }
}