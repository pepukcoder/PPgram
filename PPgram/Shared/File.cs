using PPgram.MVVM.Models.File;

namespace PPgram.Shared;

class Msg_DownloadFile
{
    public bool meta = false;
    public required FileModel file;
}
class Msg_UploadFile
{
    public required FileModel file;
}
class Msg_UploadFilesResult
{
    public bool ok;
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