using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;

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
    ReadInvisible,
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
    public required ChatModel to;
}
class Msg_EditMessage
{
    public required int chat;
    public required int Id;
    public required MessageContentModel newContent;
}
class Msg_DeleteMessage
{
    public required int chat;
    public required int Id;
}
