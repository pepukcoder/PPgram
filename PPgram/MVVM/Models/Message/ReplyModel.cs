using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.Message;

partial class ReplyModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;
    [ObservableProperty]
    private string text = string.Empty;
    [ObservableProperty]
    private int color;
}
