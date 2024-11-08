using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using PPgram.Shared;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;
using Avalonia.Media.Imaging;
using System;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.User;
using System.ComponentModel;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.File;
using PPgram.Helpers;
using System.Linq;
using PPgram.Net.DTO;
using System.Diagnostics;

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
    private ObservableCollection<SearchEntryModel> searchList = [];

    [ObservableProperty]
    private ChatModel chatListSelected = new UserModel();
    [ObservableProperty]
    private ChatModel searchListSelected = new UserModel();
    [ObservableProperty]
    private ChatModel selectedChat = new UserModel();

    [ObservableProperty]
    private string _messageInput = string.Empty;
    [ObservableProperty]
    private string _searchInput = string.Empty;
    [ObservableProperty]
    private bool _rightGridVisible;
    [ObservableProperty]
    private ProfileState profileState = ProfileState.Instance;
    private readonly DispatcherTimer _timer;
    private MessageChainManager chainManager = new();
    public ChatViewModel()
    {
        RightGridVisible = false;
        // search request delay timer
        _timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
        _timer.Tick += SearchChat;
    }
    partial void OnSearchListSelectedChanged(ChatModel value) => SelectedChat = value;
    partial void OnChatListSelectedChanged(ChatModel value) => SelectedChat = value;
    partial void OnSelectedChatChanged(ChatModel value)
    {
        if (value.Messages.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new Msg_FetchMessages()
            {
                chatId = value.Id,
            });
            
        }
        else
        {
            MessageList = new(value.Messages);
        }
    }
    partial void OnSearchInputChanged(string value)
    {
        // stop timer when editing search query
        _timer.Stop();
        // restart delay if username is not null
        // clean searchlist if search closed
        if (!String.IsNullOrEmpty(value.Trim())) _timer.Start();
        else SearchList = [];
    }
    private void SearchChat(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        _timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_SearchChats { searchQuery = SearchInput.Trim() });
    }
    public void UpdateSearch(ObservableCollection<SearchEntryModel> resultList) => SearchList = resultList;
    public void UpdateChats(ObservableCollection<ChatModel> chatList) => ChatList = chatList;
    public void UpdateMessages(ObservableCollection<MessageModel> messageList)
    {
        SelectedChat.Messages = messageList;
        MessageList = new(SelectedChat.Messages);
    }
    [RelayCommand]
    private void SendMessage()
    {
        // mockup for testing
        MessageModel message = new()
        {
            Reply = new()
            {
                Name = "Павло Потужний",
                Color = 0,
                Text = "asdasdasdasd",
            },
            Content = new TextContentModel()
            {
                Text = MessageInput.Trim(),
            },
            Role = MessageRole.OwnFirst,
            Sender = ProfileState.Name,
            Color = 3,
            SenderId = ProfileState.UserId,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Status = MessageStatus.Delivered
        };
        MessageList.Add(message);
        /*
        WeakReferenceMessenger.Default.Send(new Msg_SendMessage()
        {
            message = message,
            to = SelectedChat.Id
        });
        */
        MessageInput = "";
    }
    [RelayCommand]
    private void ClearSearch()
    {
        SearchInput = string.Empty;
        SearchList = [];
    }
}