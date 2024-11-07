using PPgram.MVVM.Models.File;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.MessageContent;

internal class MediaContentModel : MessageContentModel
{
    public ObservableCollection<MediaFileModel> MediaFiles { get; set; } = [];
    public string Text { get; set; } = string.Empty;
}
