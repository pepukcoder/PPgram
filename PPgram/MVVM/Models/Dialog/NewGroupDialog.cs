using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.File;
using PPgram.Shared;
using System;

namespace PPgram.MVVM.Models.Dialog;

internal partial class NewGroupDialog : Dialog
{
    [ObservableProperty]
    private string name = string.Empty;
    [ObservableProperty]
    private string username = string.Empty;
    [ObservableProperty]
    private PhotoModel photo = new() 
    { 
        Preview = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar_group.png", UriKind.Absolute)))
            .CreateScaledBitmap(new(150, 150), BitmapInterpolationMode.MediumQuality) 
    };
    public void SetPhoto(PhotoModel photo)
    {
        Photo = photo;
    }
    [RelayCommand]
    private void CreateGroup()
    {
        if (string.IsNullOrEmpty(Name)) return;
        WeakReferenceMessenger.Default.Send(new Msg_CreateGroup { name = Name, username = $"@{Username}", photo = Photo });
        Close();
    }
    [RelayCommand]
    private void Close()
    {
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
    }
}
