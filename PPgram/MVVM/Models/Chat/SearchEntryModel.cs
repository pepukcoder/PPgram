using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;
using System.Collections.Specialized;
using System.Linq;

namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsenets properties of a search entry
/// </summary>
/// <remarks>
/// Possibly useless, but i don't care
/// </remarks>
internal partial class SearchEntryModel : ChatModel
{
    protected override void UpdateLastMessage()
    {
        LastMessage = "";
        LastMessageStatus = MessageStatus.None;
    }
}
