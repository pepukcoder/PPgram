﻿using PPgram.MVVM.Models.Message;
using System.Collections.ObjectModel;

namespace PPgram.Shared;

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
