using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
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
    public required int to;
}
class Msg_EditMessage
{
    public required int chat;
    public required int Id;
    public required MessageContentModel newContent;
}
class Msg_EditMessageEvent
{
    public required MessageDTO? message;
}
class Msg_DeleteMessage
{
    public required int chat;
    public required int Id;
}
class Msg_DeleteMessageEvent
{
    public required int chat;
    public required int Id;
}
class Msg_NewMessage
{
    public required MessageDTO? message;
}
class Msg_ChangeMessageStatus
{
    public required int chat;
    public required int Id;
    public required MessageStatus status;
}
