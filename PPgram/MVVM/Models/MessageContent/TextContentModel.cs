using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.MessageContent;

internal partial class TextContentModel : MessageContentModel, ITextContent
{
    [ObservableProperty]
    public string text = string.Empty;
}
