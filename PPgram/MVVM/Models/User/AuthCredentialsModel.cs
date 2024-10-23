// Basic Model for Authentication
// Required to store the provided by the API UserId and SessionId in a json file 
public class AuthCredentialsModel
{
    public int UserId { get; set; }
    public required string SessionId { get; set; }
}
