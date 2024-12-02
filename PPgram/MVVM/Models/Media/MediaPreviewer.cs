using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.File;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Media;

internal partial class MediaPreviewer : ObservableObject
{
    [ObservableProperty]
    private bool visible;
    [ObservableProperty]
    private bool videoControlVisible;
    [ObservableProperty]
    private bool paused;
    [ObservableProperty]
    private bool fullscreen;

    [ObservableProperty]
    private uint mediaCount;
    [ObservableProperty]
    private uint volume = 100;

    [ObservableProperty]
    private ObservableCollection<MessageModel> messages = [];
    [ObservableProperty]
    private MessageModel currentMessage = new();
    [ObservableProperty]
    private FileModel currentFile = new();

    private static void Reset() => WeakReferenceMessenger.Default.Send(new Msg_ResetPreviewer());
    public void Open(ObservableCollection<MessageModel> messages, MessageModel message, FileModel file)
    {
        Messages = messages;
        CurrentMessage = message;
        CurrentFile = file;

        //calculate all media in chat

    }

    [RelayCommand]
    private void MoveNext()
    {
        Reset();
    }
    [RelayCommand]
    private void MovePrevious()
    {
        Reset();
    }
    [RelayCommand]
    private void Close()
    {
        Paused = true;
        Visible = false;
    }
}
