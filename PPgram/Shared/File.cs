using PPgram.MVVM.Models.File;

namespace PPgram.Shared;

class Msg_DownloadFile
{
    public required FileModel file;
}
class Msg_UploadFile
{
    public required FileModel file;
}
public enum FileStatus
{
    NotLoaded,
    Loading,
    Loaded
}
public enum DownloadMode
{
    media_only,
    preview_only,
    full
}