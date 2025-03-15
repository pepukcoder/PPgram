using PPgram.Net.DTO;

namespace PPgram.Shared;

class Msg_NewChatEvent
{
    public required ChatDTO? chat;
}
class Msg_NewMessageEvent
{
    public required int chat;
    public required MessageDTO? message;
}
class Msg_EditMessageEvent
{
    public required int chat;
    public required MessageDTO? message;
}
class Msg_DeleteMessageEvent
{
    public required int chat;
    public required int id;
}
class Msg_MarkAsReadEvent
{
    public required int chat;
    public required int[] ids;
}
class Msg_IsTypingEvent
{
    public required bool typing;
    public required int chat;
    public required int user;
}
class Msg_EditProfileEvent
{
    public required ProfileDTO profile;
}