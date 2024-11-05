using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace PPgram.MVVM.Models.User;

/// <summary>
/// Repsenets properties of any profile that can be shown in UI
/// </summary>
internal class ProfileModel
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Bitmap Avatar { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
}