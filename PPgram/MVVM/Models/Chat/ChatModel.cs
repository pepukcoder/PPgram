using System.Collections.ObjectModel;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.User;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsesents common properties of a chat
/// </summary>
internal abstract class ChatModel
{
    public int Id { get; set; }
    public int UnreadCount { get; set; }
    public long Date { get; set; }
    public ChatType Type { get; set; }
    public ChatStatus Status { get; set; }
    public MessageStatus MessageStatus { get; set; }
    public ProfileModel Profile { get; set; } = new();
    public ObservableCollection<ChatItem> Messages { get; set; } = [];
}
