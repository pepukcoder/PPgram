﻿using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;

namespace PPgram.MVVM.ViewModels;

internal partial class ProfileSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial ProfileModel Profile { get; set; } = new();
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int[] Colors { get; set; } = [.. Enumerable.Range(1, 21)];
    [ObservableProperty]
    public partial MessageModel PreviewMessage { get; set; } = new();

    private readonly AppState settings = AppState.Instance;
    private readonly ProfileState profileState = ProfileState.Instance;
    private bool avatarChanged;
    public void Load()
    {
        if (profileState.Profile == null) return;
        // get current state values
        avatarChanged = false;
        Profile.Name = profileState.Profile.Name;
        Profile.Username = profileState.Profile.Username;
        Profile.Color = profileState.Profile.Color;
        Profile.Avatar = profileState.Profile.Avatar;

        // update message preview
        PreviewMessage.Sender = Profile;
        PreviewMessage.Reply.Sender = Profile;
        PreviewMessage.Reply.Text = "Your previous message";
        PreviewMessage.Role = MessageRole.GroupFirst;
        PreviewMessage.Content = new TextContentModel { Text = "Selected color will be applied to your name and replies to your messages" };
    }
    public void SetAvatar(PhotoModel photo)
    {
        Profile.Avatar = photo;
        avatarChanged = true;
    }
    [RelayCommand]
    private void SaveProfile()
    {
        if (profileState.Profile == null) return;
        WeakReferenceMessenger.Default.Send(new Msg_EditSelf
        {
            profile = Profile,
            avatarChanged = avatarChanged
        });
        profileState.Profile.Name = Profile.Name;
        profileState.Profile.Username = Profile.Username;
        profileState.Profile.Color = Profile.Color;
        profileState.Profile.Avatar = Profile.Avatar;
    }
    [RelayCommand]
    private static void Close() => WeakReferenceMessenger.Default.Send(new Msg_ToChat());
}
