using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Shared;

namespace PPgram.MVVM.Models.File;

internal partial class FileModel : ObservableObject
{
    public string? Hash { get; set; }
    public string Path { get; set; } = string.Empty;
    [ObservableProperty]
    private string name = string.Empty;
    [ObservableProperty]
    private long size;
    [ObservableProperty]
    private long sizeLoaded;
    [ObservableProperty]
    private FileStatus status;
}
