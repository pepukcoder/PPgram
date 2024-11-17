namespace PPgram.MVVM.Models.MessageContent;

internal class TextContentModel : MessageContentModel, ITextContent
{
    public string Text { get; set; } = string.Empty;
}
