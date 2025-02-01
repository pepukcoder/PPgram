using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;

namespace PPgram.Shared;

class Msg_FetchMessages
{
    public required ChatModel chat;
    public required MessageModel anchor;
    public required int index; 
}
class Msg_SearchUsers
{
    public required string query;
}