using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PPgram.Shared;
using System;

namespace PPgram.MVVM.Models.Message;

internal class FileModel
{
    public string Name { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public long SizeLoaded { get; set; }
    public FileStatus Status { get; set; }
    public Bitmap? Preview { get; set; }
}
