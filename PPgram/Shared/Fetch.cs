namespace PPgram.Shared;

class Msg_FetchMessages
{
    public required int chatId;
    public int[] range = [-1, -99];
}
class Msg_SearchUsers
{
    public required string query;
}