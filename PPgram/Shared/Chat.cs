using PPgram.MVVM.Models.Message;
using System.Collections.Generic;

namespace PPgram.Shared;

class Msg_SendRead
{
    public required List<MessageModel> messages;
}
class Msg_SendDraft
{
    public required string draft;
    public required int chat_id;
}
public enum ChatType
{
    Chat,
    Group,
    Channel
}
public enum ChatStatus
{
    None,
    Typing,
}
public enum GroupRole
{
    None,
    Admin,
    Owner
}