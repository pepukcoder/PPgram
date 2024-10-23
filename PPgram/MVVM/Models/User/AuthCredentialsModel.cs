// Basic Model for Authentication
// Required to store the provided by the API UserId and SessionId in a json file 
internal class AuthCredentialsModel
{
    public int UserId { get; set; }
    public required string SessionId { get; set; }
}
