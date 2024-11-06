using PPgram.MVVM.Models.Message;

namespace PPgram.Shared;
public enum MessageRole
{
    User,
    UserFirst,
    Own,
    OwnFirst,
    Group,
    GroupSingle,
    GroupFirst,
    GroupLast,
}
public enum MessageStatus
{
    None,
    Sending,
    Delivered,
    Read,
    Error
}
public enum MediaType
{
    None,
    Images,
    Files,
}
class Msg_SendMessage
{
    public MessageModel message = new();
    public int to;
}