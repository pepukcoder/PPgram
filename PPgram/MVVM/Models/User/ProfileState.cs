﻿using System;

namespace PPgram.MVVM.Models.User;

/// <summary>
/// Singleton profile to keep current user data synchronized across the app
/// </summary>
internal sealed class ProfileState : ProfileModel
{
    private static readonly Lazy<ProfileState> lazy = new(() => new ProfileState());
    public static ProfileState Instance => lazy.Value;
    private ProfileState() { }
    public int UserId { get; set; }
}
