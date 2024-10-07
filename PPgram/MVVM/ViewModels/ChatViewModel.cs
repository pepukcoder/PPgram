using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PPgram.MVVM.Models;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<MessageModel> messageList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> chatList = [];
    [ObservableProperty]
    private string _messageInput;
    [ObservableProperty]
    private string _searchInput;
    [ObservableProperty]
    private bool _rightGridVisible;
    public ChatViewModel()
    {
        RightGridVisible = false;
        ChatList.Add(new ChatModel
        {
            Name = "Pepuk",
            Username = "@pepukcoder",
            LastMessage = "hehehe",
            UnreadCount = 12,
            Online = true
        });
        ChatList.Add(new ChatModel
        {
            Name = "Someone???",
            LastMessage = "is there anything sussy",
            Username = "@whoami"
        });
        ChatList.Add(new ChatModel
        {
            Name = "Pavlo",
            Username = "@pavloalpha",
            LastMessage = "asdasdasd",
            Status = MessageStatus.Delivered
        });
        ChatList.Add(new ChatModel
        {
            Name = "Artem",
            Username = "@gay",
            LastMessage = "asdasdasdasdasd",
            Status = MessageStatus.Read
        });
        ChatList.Add(new ChatModel
        {
            Name = "Itea",
            Username = "@tea",
            LastMessage = "asdasd",
            Status = MessageStatus.Sending
        });
        ChatList.Add(new ChatModel
        {
            Name = "Illiah",
            Username = "@iii",
            LastMessage = "asdasd",
            Status = MessageStatus.Error
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
            Media = [
                new() {
                    Name = "file.zip",
                    Size = 1200
                },
                new() {
                    Name = "somethings.zip",
                    Size = 5000000
                }
            ],
            MediaType = MediaType.Files,
            Type = MessageType.Group
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
        MessageList.Add(new MessageModel
        {
            Text = "this is the proof",
            MediaType = MediaType.Files,
            Type = MessageType.Own,
            Status = MessageStatus.Delivered,
            Media = [
                new() {
                    Name = "file.txt",
                    Size = 500,
                    HasPreview = false
                },
                new() {
                    Name = "proof.png",
                    Size = 2000,
                    HasPreview = false
                }
            ],
            Edited = true
        });
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
    [RelayCommand]
    private void SendMessage()
    {
        MessageInput = "";
    }
    [RelayCommand]
    private void SearchChat()
    {
        ClearSearch();
    }
    [RelayCommand]
    private void ClearSearch()
    {
        SearchInput = "";
    }
}