using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;
using System.Linq;

namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsenets properties of a user chat
/// </summary>
internal partial class UserModel : ChatModel
{
    [ObservableProperty]
    private bool online;
    [ObservableProperty]
    private bool showOnline = true;

    protected override void UpdateLastMessage()
    {
        MessageModel? lastmsg = Messages.OfType<MessageModel>().LastOrDefault();
        if (lastmsg == null)
        {
            LastMessage = "";
            LastMessageStatus = MessageStatus.None;
            return;
        }
        if (lastmsg.Content is ITextContent tc) LastMessage = tc.Text;
        if (lastmsg.SenderId == profileState.UserId) LastMessageStatus = lastmsg.Status;
        else LastMessageStatus = MessageStatus.None;
        LastMessageTime = lastmsg.Time;
    }
    protected override void UpdateStatus()
    {
        if (Status == ChatStatus.Typing) ShowOnline = false;
        else if (Status == ChatStatus.None) ShowOnline = true;
    }
}
