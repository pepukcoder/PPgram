using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.MessageContent;

internal abstract class MessageContentModel : ObservableObject
{
}
public interface ITextContent
{
    string Text { get; set; }
}