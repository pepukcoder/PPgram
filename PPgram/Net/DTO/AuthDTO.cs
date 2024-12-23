using System.Text.Json.Serialization;

namespace PPgram.Net.DTO;

internal class AuthDTO
{
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
}
