using PPgram.Net.DTO;
using System.Collections.Generic;

namespace PPgram.Shared;

class Msg_FetchMessages
{
    public required int chatId;
    public int[] range = [-1, -99];
}
class Msg_FetchSelfResult
{
    public required ProfileDTO? profile;
}
class Msg_FetchUserResult
{
    public required ProfileDTO? user;
}
class Msg_FetchChatsResult
{
    public required List<ChatDTO> chats;
}
class Msg_FetchMessagesResult
{
    public required List<MessageDTO> messages;
}