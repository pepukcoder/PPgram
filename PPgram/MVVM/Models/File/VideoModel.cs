using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.File;

internal partial class VideoModel : FileModel
{
    [ObservableProperty]
    private Bitmap? preview;
    [ObservableProperty]
    private uint length;
}
