using System;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;

namespace PPgram.MVVM.ViewModels;

internal partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap avatar;
    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private string username;
    [ObservableProperty]
    private int[] colors;
    [ObservableProperty]
    private int color;
    [ObservableProperty]
    private MessageModel previewMessage;

    private readonly AppState settings = AppState.Instance;
    private readonly ProfileState profile = ProfileState.Instance;
    public SettingsViewModel()
    {
        // assign default values
        Name = string.Empty;
        Username = string.Empty;
        Avatar = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
        Colors = [0,1,2,3,4,5,6];
        PreviewMessage = new();
    }
    public void Update()
    {
        // get current state values
        Avatar = profile.Avatar;
        Name = profile.Name;
        Username = profile.Username;
        Color = profile.Color;
        // update message preview
        // TODO: store color as ref type to ensure sync
        PreviewMessage.Sender.Avatar = Avatar;
        PreviewMessage.Sender.Name = Name;
        PreviewMessage.Sender.Color = Color;
        PreviewMessage.Reply.Name = Name;
        PreviewMessage.Reply.Text = "Your previous message";
        PreviewMessage.Reply.Color = Color;
        PreviewMessage.Role = MessageRole.GroupFirst;
        PreviewMessage.Content = new TextContentModel { Text = "Your message" };
    }
    [RelayCommand]
    private void Save()
    {
        // send updated profile to server && save app settings to FS
    }
    [RelayCommand]
    private static void Close() => WeakReferenceMessenger.Default.Send(new Msg_ToChat());
}
