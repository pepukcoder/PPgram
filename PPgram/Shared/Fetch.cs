using PPgram.Net.DTO;
using System.Collections.Generic;

namespace PPgram.Shared;

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
    public List<ChatDTO> chats;
}