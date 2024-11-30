using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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


    [RelayCommand]
    private void Close()
    {
        Paused = true;
        Visible = false;
    }
}
