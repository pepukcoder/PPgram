using System.Collections.ObjectModel;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using System.Collections.Specialized;
using System.Linq;
using PPgram.MVVM.Models.User;
using PPgram.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsenets properties of a group chat
/// </summary>
internal partial class GroupModel : ChatModel
{
    [ObservableProperty]
    public string lastSender = "";
    [ObservableProperty]
    private ObservableCollection<GroupMemberModel> members = [];

    public bool Private { get; set; }
    public string Link { get; set; } = string.Empty;
    
    protected override void UpdateLastMessage(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MessageModel? lastmsg = Messages.OfType<MessageModel>().LastOrDefault();
        if (lastmsg == null)
        {
            LastMessage = "";
            LastSender = "";
            LastMessageStatus = MessageStatus.None;
            return;
        }
        if (lastmsg.Content is ITextContent tc) LastMessage = tc.Text;
        if (lastmsg.SenderId == profileState.UserId)
        {
            LastSender = "You";
            LastMessageStatus = lastmsg.Status;
        }
        else
        {
            LastSender = lastmsg.Sender.Name;
            LastMessageStatus = MessageStatus.None;
        }
    }
}
