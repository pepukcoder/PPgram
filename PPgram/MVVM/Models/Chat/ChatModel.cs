using System.Collections.ObjectModel;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.User;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Chat;

internal abstract class ChatModel
{
    public int Id { get; set; }
    public int UnreadCount { get; set; }
    public string Date { get; set; } = "00:00";
    public ChatType Type { get; set; }
    public ChatStatus Status { get; set; }
    public MessageStatus MessageStatus { get; set; }
    public ProfileModel Profile { get; set; } = new();
    public ObservableCollection<MessageModel> Messages { get; set; } = [];
}
