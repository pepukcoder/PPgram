using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PPgram.MVVM.Models;
using System.Collections.ObjectModel;
namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<MessageModel> messages = [];
    [ObservableProperty]
    private string _messageInput;
    public ChatViewModel()
    {
        Messages.Add(new MessageModel
        {
            Text = "hello",
            Type = MessageType.GroupFirst
        });
        Messages.Add(new MessageModel
        {
            Text = "!!!",
            Type = MessageType.Group
        });
        Messages.Add(new MessageModel
        {
            Text = "who is pavlo",
            Type = MessageType.GroupLast,
            Edited = true
        });
        Messages.Add(new MessageModel
        {
            Text = "gay",
            Type = MessageType.OwnFirst,
            Status = MessageStatus.Read
        });
        Messages.Add(new MessageModel
        {
            Text = "hehehe",
            Type = MessageType.Own,
            Status = MessageStatus.Delivered,
            Edited = true
        });
        Messages.Add(new MessageModel
        {
            Text = "okay that was personal",
            Type = MessageType.Own,
            Status = MessageStatus.Sending,
        });
        Messages.Add(new MessageModel
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