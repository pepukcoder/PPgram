namespace PPgram.MVVM.Models.Chat;

internal class UserModel : ChatModel
{
    public bool Online { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public string LastSender { get; } = string.Empty;
    public string LastOnline { get; set; } = string.Empty;
}
