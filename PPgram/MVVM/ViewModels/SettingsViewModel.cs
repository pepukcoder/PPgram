using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Shared;

namespace PPgram.MVVM.ViewModels;

internal partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ProfileModel profile;
    [ObservableProperty]
    private int[] colors;
    [ObservableProperty]
    private MessageModel previewMessage;

    private readonly AppState settings = AppState.Instance;
    private readonly ProfileState profileState = ProfileState.Instance;
    public SettingsViewModel()
    {
        // assign default values
        Colors = [.. Enumerable.Range(1, 21)];
        Profile = new();
        PreviewMessage = new();
    }
    public void Update()
    {
        // get current state values
        Profile = profileState;
        // update message preview
        PreviewMessage.Sender = Profile;
        PreviewMessage.Reply.Sender = Profile;
        PreviewMessage.Reply.Text = "Your previous message";
        PreviewMessage.Role = MessageRole.GroupFirst;
        PreviewMessage.Content = new TextContentModel { Text = "Selected color will be applied to your name and replies to your messages" };
    }
    [RelayCommand]
    private void Save()
    {
        // send updated profile to server && save app settings to FS
    }
    [RelayCommand]
    private static void Close() => WeakReferenceMessenger.Default.Send(new Msg_ToChat());
}
