using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using PPgram.Shared;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;

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
            Date = "00:00",
            Online = false
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
    private void ClearSearch() => SearchInput = string.Empty;
}