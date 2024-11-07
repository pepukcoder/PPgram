using PPgram.MVVM.Models.File;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.MessageContent;

internal class FileContentModel : MessageContentModel
{
    public ObservableCollection<FileModel> Files { get; set; } = [];
    public string Text { get; set; } = string.Empty;
}
