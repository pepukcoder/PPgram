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
            case Shared.FileStatus.NotLoaded:
                file.Status = Shared.FileStatus.Loading;
                WeakReferenceMessenger.Default.Send(new Msg_DownloadFile { file = file });
                break;
            case Shared.FileStatus.Loading:
                file.Status = Shared.FileStatus.NotLoaded;
                // TODO: abort loading thread
                break;
            case Shared.FileStatus.Loaded:
                // TODO: call previewer
                break;
        }
    }
}
