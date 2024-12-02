using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.File;

internal partial class PhotoModel : FileModel
{
    [ObservableProperty]
    private Bitmap? preview;
    [ObservableProperty]
    private bool compress;
}
