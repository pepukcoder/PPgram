using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace PPgram.MVVM.Models.User;

/// <summary>
/// Repsenets properties of any profile that can be shown in UI
/// </summary>
internal partial class ProfileModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;
    [ObservableProperty]
    private string username= string.Empty;
    [ObservableProperty]
    private Bitmap avatar = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    [ObservableProperty]
    private int color;
}