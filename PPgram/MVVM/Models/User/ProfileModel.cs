using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.File;
using System;

namespace PPgram.MVVM.Models.User;

/// <summary>
/// Repsenets properties of user profile
/// </summary>
internal partial class ProfileModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;
    [ObservableProperty]
    private string username = string.Empty;
    [ObservableProperty]
    private PhotoModel avatar = new();
    [ObservableProperty]
    private int color;
}