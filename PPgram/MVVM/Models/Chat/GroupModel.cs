using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;
using System.Collections.ObjectModel;
using System.Linq;

namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsenets properties of a group chat
/// </summary>
internal partial class GroupModel : ChatModel
{
    [ObservableProperty]
    private string lastSender = string.Empty;
    [ObservableProperty]
    private ObservableCollection<GroupMemberModel> members = [];
    [ObservableProperty]
    private string typing = string.Empty;
    public bool Private { get; set; }
    public string Link { get; set; } = string.Empty;
    
    protected override void UpdateLastMessage()
    {
        MessageModel? lastmsg = Messages.OfType<MessageModel>().FirstOrDefault();
        if (lastmsg == null)
        {
            LastMessage = string.Empty;
            LastSender = string.Empty;
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
        LastMessageTime = lastmsg.Time;
    }
    protected override void UpdateStatus()
    {

    }
}
