using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.Message;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.MessageContent;

internal partial class FileContentModel : MessageContentModel, ITextContent
{
    public ObservableCollection<FileModel> Files { get; set; } = [];
    public bool Media { get; set; }
    [ObservableProperty]
    public string text = string.Empty;
}
