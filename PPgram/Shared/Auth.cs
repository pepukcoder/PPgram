namespace PPgram.Shared;

class Msg_ToReg;
class Msg_ToLogin;
class Msg_Logout;

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
class Msg_AuthResult
{
    public int userId;
    public string sessionId = string.Empty;
    public bool auto = false;
}