using System;
using PPgram.MVVM.Models.User;

namespace PPgram.App;

/// <summary>
/// Singleton that represents current user data synchronized across the app
/// </summary>
internal sealed class ProfileState
{
    private static readonly Lazy<ProfileState> lazy = new(() => new ProfileState());
    public static ProfileState Instance => lazy.Value;
    private ProfileState() { }
    public int UserId { get; set; }
    public ProfileModel? Profile {get; set;}
}
