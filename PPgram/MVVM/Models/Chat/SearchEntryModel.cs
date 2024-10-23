using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Chat;

internal class SearchEntryModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Bitmap Avatar { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public ChatType Type { get; set; }
}
