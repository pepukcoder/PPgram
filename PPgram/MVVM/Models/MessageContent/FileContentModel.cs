using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using PPgram.MVVM.Models.File;

namespace PPgram.MVVM.Models.MessageContent;

internal partial class FileContentModel : MessageContentModel, ITextContent
{
    public ObservableCollection<FileModel> Files { get; set; } = [];
    [ObservableProperty]
    public string text = string.Empty;
}
