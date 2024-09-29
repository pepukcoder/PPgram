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
    
    public ChatViewModel()
    {
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
            LastMessage = "פûגפûגפûגפûגפûג",
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
            Type = MessageType.Group
        });
        MessageList.Add(new MessageModel
        {
            AttachmentName = "asd.asd",
            AttachmentHash = "asdasd",
            Type = MessageType.GroupLast,
            Edited = true
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
            AttachmentName = "proof.zip",
            AttachmentHash = "asdasd",
            Text = "this is the proof",
            Type = MessageType.Own,
            Status = MessageStatus.Delivered,
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
}