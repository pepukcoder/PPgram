using PPgram.Net.DTO;

namespace PPgram.Shared;

class Msg_NewChatEvent
{
    public required ChatDTO? chat;
}
class Msg_NewMessageEvent
{
    public required MessageDTO? message;
}
class Msg_EditMessageEvent
{
    public required MessageDTO? message;
}
class Msg_DeleteMessageEvent
{
    public required int chat;
    public required int Id;
}