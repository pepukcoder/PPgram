using System.Text.Json.Serialization;

namespace PPgram.Net.DTO;

internal class ChatDTO
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("username")]
    public string? Username { get; set; }
    [JsonPropertyName("chat_id")]
    public int? Id { get; set; }
    [JsonPropertyName("photo")]
    public string? Photo { get; set; }
}
