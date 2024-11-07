using PPgram.Shared;

namespace PPgram.MVVM.Models.File;

internal class FileModel
{
    public string Name { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long Size { get; set; }
    public long SizeLoaded { get; set; }
    public FileStatus Status { get; set; }
}
