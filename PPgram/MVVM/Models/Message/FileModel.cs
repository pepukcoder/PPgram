using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Message;

internal partial class FileModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string? Hash { get; set; }
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    [ObservableProperty]
    public long sizeLoaded;
    [ObservableProperty]
    public FileStatus status;
    [ObservableProperty]
    public Bitmap? preview;
}
