namespace PPgram.Shared;

class Msg_ToReg;
class Msg_ToLogin;
class Msg_Logout;

class Msg_Login
{
    public required string username;
    public required string password;
}
class Msg_Register
{
    public string name = string.Empty;
    public required string username;
    public string password = string.Empty;
    public bool check = false;
}
class Msg_AuthResult
{
    public int userId;
    public string sessionId = string.Empty;
    public bool auto = false;
}
class Msg_CheckResult
{
    public bool available;
}