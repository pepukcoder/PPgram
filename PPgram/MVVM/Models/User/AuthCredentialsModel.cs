namespace PPgram.MVVM.Models.User;

/// <summary>
/// Model for Authentication to store UserId and SessionId provided by the API in a json file
/// </summary>
internal class AuthCredentialsModel
{
    public int UserId { get; set; }
    public required string SessionId { get; set; }
}
