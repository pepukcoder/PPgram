namespace PPgram.Shared;

class Msg_Auth
{
    public string username = string.Empty;
    public string password = string.Empty;
    public string name = string.Empty;
    public bool auto = false;
}
class Msg_CheckUsername
{
    public required string username;
}