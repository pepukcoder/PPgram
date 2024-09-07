using System;
using Avalonia.Media.Imaging;

namespace PPgram.MVVM.Models;

internal sealed class ProfileState
{
    private static readonly Lazy<ProfileState> lazy = new(() => new ProfileState());
    public static ProfileState Instance => lazy.Value;
    private ProfileState() { }

    public string Name { get; set; }
    public string Username { get; set; }
    public Bitmap Avatar { get; set; }
    public int Id { get; set; }

}
