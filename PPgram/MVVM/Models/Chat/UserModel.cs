namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsenets properties of a user chat
/// </summary>
internal class UserModel : ChatModel
{
    public bool Online { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public string LastOnline { get; set; } = string.Empty;
}
