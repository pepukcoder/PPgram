using PPgram.Net.DTO;
using System.Collections.Generic;

namespace PPgram.Shared;

class Msg_FetchMessages
{
    public int chatId;
    public int[] range = [-1, -99];
}
class Msg_FetchSelfResult
{
    public ProfileDTO? profile;
}
class Msg_FetchUserResult
{
    public ProfileDTO? user;
}
class Msg_FetchChatsResult
{
    public List<ChatDTO> chats = [];
}
class Msg_FetchMessagesResult
{
    public List<MessageDTO> messages = [];
}