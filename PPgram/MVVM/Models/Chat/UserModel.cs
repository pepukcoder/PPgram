namespace PPgram.MVVM.Models.Chat;

internal class UserModel : ChatModel
{
    public bool Online { get; set; }
    public string LastOnline { get; set; } = string.Empty;
}
