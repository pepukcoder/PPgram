using System;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace PPgram.MVVM.Models;

internal sealed class ProfileState
{
    private static readonly Lazy<ProfileState> lazy = new(() => new ProfileState());
    public static ProfileState Instance => lazy.Value;
    private ProfileState() { }

    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Bitmap Avatar { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public int Id { get; set; }

}
