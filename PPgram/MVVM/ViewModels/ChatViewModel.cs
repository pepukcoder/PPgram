using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using PPgram.MVVM.Models;
using PPgram.Shared;

namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<MessageModel> messageList = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> chatList = [];
    [ObservableProperty]
    private string _messageInput = string.Empty;
    [ObservableProperty]
    private string _searchInput = string.Empty;
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