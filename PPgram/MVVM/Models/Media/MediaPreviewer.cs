using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PPgram.MVVM.Models.Message;

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
    private uint volume = 100;

    [ObservableProperty]
    private ObservableCollection<MessageModel> messages = [];
    [ObservableProperty]
    private MessageModel currentMessage = new();
    [ObservableProperty]
    private FileModel currentFile = new();

    public void Open(ObservableCollection<MessageModel> messages, FileModel currentFile)
    {
        
    }

    [RelayCommand]
    private void MoveNext()
    {

    }
    [RelayCommand]
    private void MovePrevious()
    {

    }
    [RelayCommand]
    private void Close()
    {
        Paused = true;
        Visible = false;
    }
}
