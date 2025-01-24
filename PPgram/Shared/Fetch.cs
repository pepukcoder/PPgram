using PPgram.MVVM.Models.Message;

namespace PPgram.Shared;

class Msg_FetchMessages
{
    public required int index; 
    public required MessageModel anchor;
}
class Msg_SearchUsers
{
    public required string query;
}