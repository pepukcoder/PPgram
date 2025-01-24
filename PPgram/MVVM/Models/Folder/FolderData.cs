using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PPgram.MVVM.Models.Folder;

internal class FolderData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Folder";
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("chats")]
    public HashSet<int> Chats { get; set; } = [];
}
