using PPgram.MVVM.Models.File;
using System.Collections.ObjectModel;

namespace PPgram.Shared;

class Msg_DownloadFile
{
    public bool meta = false;
    public required FileModel file;
}
class Msg_UploadFiles
{
    public required ObservableCollection<FileModel> files;
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
