using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
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
    public partial string Name { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Tag { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string TagStatus { get; set; } = string.Empty;
    [ObservableProperty]
    public partial PhotoModel Photo { get; set; } = new() 
    { 
        Preview = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar_group.png", UriKind.Absolute)))
    };
    private bool tagOk = true;
    private readonly DispatcherTimer timer;
    public NewGroupDialog()
    {
        // tag check request delay timer
        timer = new() { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += CheckTag;
    }
    partial void OnTagChanged(string value)
    {
        tagOk = false;
        // stop timer if user is still editing tag
        timer.Stop();
        if (String.IsNullOrEmpty(value))
        {
            tagOk = true;
        }
        else
        {
            // check if tag is valid characters
            foreach (char c in value)
            {
                if (!Char.IsAsciiLetterOrDigit(c) && c != '_' || value.StartsWith('_'))
                {
                    ShowTagStatus("Tag is invalid");
                    return;
                }
            }
            // check username length
            if (value.Length < 3)
            {
                ShowTagStatus("Tag is too short");
                return;
            }
            // restart delay if username is valid
            timer.Start();
        }
        ShowTagStatus();
    }
    private void CheckTag(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_CheckGroupTag(){ dialog = this, tag = $"@{Tag}" });
    }
    public void SetPhoto(PhotoModel photo)
    {
        Photo = photo;
    }
    public void ShowTagStatus(string status = "", bool ok = false)
    {
        TagStatus = status;
        tagOk = ok;
    }
    [RelayCommand]
    private void CreateGroup()
    {
        if (string.IsNullOrEmpty(Name) || !tagOk) return;
        WeakReferenceMessenger.Default.Send(new Msg_CreateGroup { name = Name, tag = $"@{Tag}", photo = Photo });
        Close();
    }
    [RelayCommand]
    private static void Close()
    {
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
    }
}
