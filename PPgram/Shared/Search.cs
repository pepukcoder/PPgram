using System.Collections.Generic;
using PPgram.Net.DTO;

namespace PPgram.Shared;

class Msg_SearchChats
{
    public string searchQuery = string.Empty;
}
class Msg_SearchChatsResult
{
    public List<ChatDTO> users = [];
}
