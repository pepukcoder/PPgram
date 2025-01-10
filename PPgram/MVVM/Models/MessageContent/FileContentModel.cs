using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using PPgram.MVVM.Models.File;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;

namespace PPgram.MVVM.Models.MessageContent;

internal partial class FileContentModel : MessageContentModel, ITextContent
{
    [ObservableProperty]
    public ObservableCollection<FileModel> files = [];
    [ObservableProperty]
    public string text = string.Empty;
    [RelayCommand]
    private void InteractFile(FileModel file)
    {
        switch (file.Status)
        {
            case FileStatus.NotLoaded:
                file.Status = FileStatus.Loading;
                WeakReferenceMessenger.Default.Send(new Msg_DownloadFile { file = file });
                break;
            case FileStatus.Loading:
                file.Status = FileStatus.NotLoaded;
                // TODO: abort loading thread
                break;
            case FileStatus.Loaded:
                if (file is PhotoModel || file is VideoModel) WeakReferenceMessenger.Default.Send(new Msg_OpenPreviewer { content = this, file = file });
                break;
        }
    }
}
