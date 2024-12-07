using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PPgram.MVVM.Models.Message;

internal class RequestMetadataModel
{
    public required string name;
    public required bool is_media;
    public required bool compress;
}

internal class MetadataModel
{
    [JsonPropertyName("file_name")]
    public required string FileName { get; set; }

    [JsonPropertyName("file_size")]
    public required long FileSize { get; set; }
}

internal class DownloadMetadataResponseModel
{
    [JsonPropertyName("ok")]
    public required bool Ok { get; set; }

    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("metadatas")]
    public required List<MetadataModel> Metadatas { get; set; }
}
