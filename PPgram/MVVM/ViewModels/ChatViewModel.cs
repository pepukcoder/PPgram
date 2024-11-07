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
        MessageList.Clear();
        MessageList.Add(new DateBadgeModel()
        {
            Date = DateTimeOffset.Now.ToUnixTimeSeconds()
        });
        MessageList.Add(new MessageModel()
        {
            Content = new TextContentModel()
            {
                Text = "паша альфа"
            },
            Role = MessageRole.OwnFirst,
            Sender = ProfileState.Name,
            SenderId = ProfileState.UserId,
            
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Status = MessageStatus.Read
        });
        MessageList.Add(new MessageModel()
        {
            Reply = new()
            {
                Name = ProfileState.Name,
                Color = 0,
                Text = "паша альфа",
            },
            Content = new TextContentModel()
            {
                Text = "то есть гей"
            },
            Role = MessageRole.Own,
            Sender = SelectedChat.Profile.Name,
            SenderId = 0,

            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Status = MessageStatus.Read
        });
        MessageList.Add(new MessageModel()
        {
            Reply = new()
            {
                Name = ProfileState.Name,
                Color = 3,
                Text = "то есть гей",
            },
            Content = new TextContentModel()
            {
                Text = "чзх"
            },
            Role = MessageRole.GroupFirst,
            Sender = SelectedChat.Profile.Name,
            Color = 1,
            SenderId = 0,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
        });
        MessageList.Add(new MessageModel()
        {
            Reply = new()
            {
                Name = SelectedChat.Profile.Name,
                Color = 1,
                Text = "чекай",
            },
            Content = new FileContentModel()
            {
                Files = new()
                {
                    new FileModel()
                    {
                        Name = "file 500b.zip",
                        Size = 500
                    },
                    new FileModel()
                    {
                        Name = "file 5mb.zip",
                        Size = 5000000
                    },
                },
                Text = "короче вот тебе файлики"
            },
            Role = MessageRole.GroupFirst,
            Sender = SelectedChat.Profile.Name,
            Color = 1,
            SenderId = 0,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
        });
        MessageList.Add(new MessageModel()
        {
            Content = new MediaContentModel()
            {
                MediaFiles = new()
                {
                    new MediaFileModel()
                    {
                        Preview = Base64ToBitmapConverter.ConvertBase64(null),
                        Name = "Fileasdasdasd",
                        Size = 500
                    },
                    new MediaFileModel()
                    {
                        Preview = Base64ToBitmapConverter.ConvertBase64(null),
                        Name = "asdasdasd",
                        Size = 5000
                    },
                },
                Text = "вот тебе медиа"
            },
            Role = MessageRole.GroupLast,
            Sender = SelectedChat.Profile.Name,
            Color = 1,
            SenderId = 0,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
        });
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
    public void UpdateSearch(ObservableCollection<SearchEntryModel> resultlist) => SearchList = resultlist;
    public void UpdateChats(ObservableCollection<ChatModel> chatlist) => ChatList = chatlist;
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