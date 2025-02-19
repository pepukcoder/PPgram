using System.Text.Json.Serialization;

namespace PPgram.Net;

internal class ConnectionOptions
{
    [JsonPropertyName("host_json")]
    public required string JsonHost { get; set; }
    [JsonPropertyName("host_files")]
    public required string FilesHost { get; set; }
    [JsonPropertyName("port_json")]
    public required int JsonPort { get; set; }
    [JsonPropertyName("port_files")]
    public required int FilesPort { get; set; }
}
