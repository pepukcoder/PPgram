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

namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap profileAvatar = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    [ObservableProperty]
    private string profileName = string.Empty;
    [ObservableProperty]
    private string profileUsername = string.Empty;
    [ObservableProperty]
    private Bitmap chatAvatar = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    [ObservableProperty]
    private string chatName = string.Empty;
    [ObservableProperty]
    private string chatStatus = "last seen 12:34";
    [ObservableProperty]
    private ObservableCollection<MessageModel> messageList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> chatList = [];
    [ObservableProperty]
    private ObservableCollection<SearchEntryModel> searchList = [];
    [ObservableProperty]
    private string _messageInput = string.Empty;
    [ObservableProperty]
    private string _searchInput = string.Empty;
    [ObservableProperty]
    private bool _rightGridVisible;
    private readonly ProfileState profileState = ProfileState.Instance;
    private readonly DispatcherTimer _timer;
    public ChatViewModel()
    {
        RightGridVisible = false;
        // search request delay timer
        _timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
        _timer.Tick += SearchChat;

        // mockups
        SearchList.Add(new SearchEntryModel
        {
            Name = "Papuga",
            Username = "@papuga",
            Type = ChatType.Chat
        });
        SearchList.Add(new SearchEntryModel
        {
            Name = "GayFront",
            Username = "@ppfront",
            Type = ChatType.Group
        });
        SearchList.Add(new SearchEntryModel
        {
            Name = "PPgram Official",
            Username = "@ppgram",
            Type = ChatType.Channel
        });

        ChatList.Add(new UserModel
        {
            Profile = new() { Name = "Pepuk" },
            LastMessage = "hehehe",
            UnreadCount = 12,
            Online = true
        });
        ChatList.Add(new UserModel
        {
            Profile = new() { Name = "Pavlo" },
            LastMessage = "nice",
            MessageStatus = MessageStatus.Delivered,
            Date = "12:11",
            Online = false
        });
        ChatList.Add(new GroupModel
        {
            Profile = new() { Name = "TestingGroup" },
            LastSender = "Pepuk",
            LastMessage = "hi",
            MessageStatus = MessageStatus.Read,
            Date = "15:00"
        });

        MessageList.Add(new MessageModel
        {
            Text = "hello",
            Type = MessageType.GroupFirst,
            ReplyText = "hello",
            ReplyName = "pavlo"
        });
        MessageList.Add(new MessageModel
        {
            Text = "who is pavlo?",
            Type = MessageType.GroupLast
        });
        MessageList.Add(new MessageModel
        {
            Text = "single group message",
            Type = MessageType.GroupSingle
        });
        MessageList.Add(new MessageModel
        {
            Text = "gay",
            ReplyName = "Pavlo",
            ReplyText = "who is pavlo?",
            Type = MessageType.OwnFirst,
            Status = MessageStatus.Read
        });
        // use this piece for performance checks
        for (int i = 0; i < 1; i++)
        {
            MessageList.Add(new MessageModel
            {
                Text = "here is some files",
                Media = [
                    new() {
                        Name = "asd",
                        Size = 5000000
                    },
                    new() {
                        Name = "asd",
                        Size = 5000000
                    },
                ],
                MediaType = MediaType.Files,
                Type = MessageType.Own,
                Status = MessageStatus.Delivered,
                Edited = true
            });
        }
        MessageList.Add(new MessageModel
        {
            Text = "okay that was personal",
            Type = MessageType.Own,
            Status = MessageStatus.Sending,
        });
        MessageList.Add(new MessageModel
        {
            Text = "my network is so slow why :(",
            Type = MessageType.Own,
            Status = MessageStatus.Error,
        });
    }
    partial void OnSearchInputChanged(string value)
    {
        // stop timer when editing search query
        _timer.Stop();
        // restart delay if username is not null
        if (!String.IsNullOrEmpty(value.Trim())) _timer.Start();
    }
    private void SearchChat(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        _timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_SearchChats { searchQuery = SearchInput.Trim() });
    }
    public void UpdateProfile()
    {
        ProfileAvatar = profileState.Avatar;
        ProfileName = profileState.Name;
        ProfileUsername = profileState.Username;
    }
    public void UpdateSearch(ObservableCollection<SearchEntryModel> resultlist) => SearchList = resultlist;
    [RelayCommand]
    private void SendMessage()
    {
        MessageModel message = new MessageModel
        {
            Text = MessageInput,
            Type = MessageType.Own,
            Status = MessageStatus.Sending,
        };
        MessageList.Add(message);
        MessageInput = "";
    }
    [RelayCommand]
    private void ClearSearch() => SearchInput = string.Empty;
}