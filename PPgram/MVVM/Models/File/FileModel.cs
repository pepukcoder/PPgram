using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Shared;

namespace PPgram.MVVM.Models.File;

internal partial class FileModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string? Hash { get; set; }
    public string Path { get; set; } = string.Empty;
    [ObservableProperty]
    public long size;
    [ObservableProperty]
    public long sizeLoaded;
    [ObservableProperty]
    public FileStatus status;
}
