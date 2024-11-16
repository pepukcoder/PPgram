using System.Collections.Generic;
using PPgram.Net.DTO;

namespace PPgram.Shared;

class Msg_SearchChats
{
    public required string searchQuery;
}
class Msg_SearchChatsResult
{
    public required List<ChatDTO> users;
}
