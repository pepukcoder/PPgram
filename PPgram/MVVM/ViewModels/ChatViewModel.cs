using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private string chatStatus = "last seen 12:34";

    [ObservableProperty]
    private ObservableCollection<ChatItem> messageList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> chatList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> searchList = [];

    [ObservableProperty]
    private ChatItem? messageListSelected = null;
    [ObservableProperty]
    private ChatModel? chatListSelected = new UserModel();
    [ObservableProperty]
    private ChatModel? searchListSelected = new UserModel();
    [ObservableProperty]
    private ProfileModel currentProfile = new();

    [ObservableProperty]
    private string _messageInput = string.Empty;
    [ObservableProperty]
    private string _searchInput = string.Empty;
    [ObservableProperty]
    private ReplyModel reply = new();
    [ObservableProperty]
    private bool _rightGridVisible;
    [ObservableProperty]
    private bool inReply;
    [ObservableProperty]
    private ProfileState profileState = ProfileState.Instance;
    private readonly DispatcherTimer _timer;
    private readonly MessageChainManager chainManager = new();
    private bool inSearch;
    
    public ChatViewModel()
    {
        RightGridVisible = false;
        // search request delay timer
        _timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
        _timer.Tick += SearchChat;

        WeakReferenceMessenger.Default.Register<Msg_ChangeMessageStatus>(this, (r, e) => { ChangeMessageStatus(e.chat, e.Id, e.status); });
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
        var messages = ChatList.FirstOrDefault(c => c.Id == message.Chat)?.Messages;
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
    [RelayCommand]
    private void SendMessage()
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

        // create message
        MessageModel message = new()
        {
            Chat = ChatListSelected.Id,
            Content = new TextContentModel()
            {
                Text = MessageInput.Trim(),
            },
            Role = MessageRole.OwnFirst,
            Sender = ProfileState.Name,
            Color = 3,
            SenderId = ProfileState.UserId,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Status = MessageStatus.Sending
        };
        // add reply if set
        if (InReply && MessageListSelected is MessageModel selectedMessage)
        {
            message.ReplyTo = selectedMessage.Id;
            message.Reply = Reply;
            InReply = false;
            MessageListSelected = null;
        }
        var messages = ChatList.FirstOrDefault(c => c.Id == ChatListSelected.Id)?.Messages;
        if (messages == null) return;
        messages.Add(message);
        chainManager.AddChain(messages);

        WeakReferenceMessenger.Default.Send(new Msg_SendMessage()
        {
            message = message,
            to = ChatListSelected.Id
        });
        MessageInput = "";
    }
    [RelayCommand]
    private void ClearSearch()
    {
        SearchInput = string.Empty;
        inSearch = false;
    }
    [RelayCommand]
    private void DeleteMessage()
    {
        if (MessageListSelected != null && MessageListSelected is MessageModel message)
        {
            MessageList.Remove(MessageListSelected);
            WeakReferenceMessenger.Default.Send(new Msg_DeleteMessage
            {
                chat = message.Chat,
                Id = message.Id
            });
        }    
    }
    [RelayCommand]
    private void AddReply()
    {
        if (MessageListSelected != null && MessageListSelected is MessageModel message)
        {
            InReply = true;
            if (message.SenderId == ProfileState.UserId) Reply.Name = ProfileState.Name;
            else Reply.Name = ChatListSelected?.Profile.Name ?? string.Empty;
            
            if (message.Content is TextContentModel textcontent) Reply.Text = textcontent.Text;

            Debug.WriteLine(Reply.Name);
            Debug.WriteLine(Reply.Text);
        }
    }
    [RelayCommand]
    private void RemoveReply() => InReply = false;
}