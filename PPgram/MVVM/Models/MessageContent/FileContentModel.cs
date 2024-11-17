using PPgram.MVVM.Models.Message;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.MessageContent;

internal class FileContentModel : MessageContentModel, ITextContent
{
    public ObservableCollection<FileModel> Files { get; set; } = [];
    public bool Media { get; set; }
    public string Text { get; set; } = string.Empty;
}
