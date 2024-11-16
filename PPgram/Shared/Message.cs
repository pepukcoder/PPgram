using PPgram.MVVM.Models.Message;
using PPgram.Net.DTO;

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
public enum ContentType
{
    Text,
    Images,
    Files,
}
class Msg_SendMessage
{
    public required MessageModel message;
    public int to;
}
class Msg_NewMessage
{
    public MessageDTO? message;
}
class Msg_ChangeMessageStatus
{
    public required int chat;
    public required int Id;
    public required MessageStatus status;
}
class Msg_DeleteMessage
{
    public required int chat;
    public required int Id;
}