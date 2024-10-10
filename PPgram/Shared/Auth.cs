namespace PPgram.Shared;

class Msg_ToReg;
class Msg_Login
{
    public string username = string.Empty;
    public string password = string.Empty;
}
class Msg_ToLogin;
class Msg_Register
{
    public string name = string.Empty;
    public string username = string.Empty;
    public string password = string.Empty;
    public bool check = false;
}
class Msg_AuthResult
{
    public bool auto;
    public int userId;
    public string sessionId = string.Empty;
}
class Msg_CheckResult
{
    public bool available;
}