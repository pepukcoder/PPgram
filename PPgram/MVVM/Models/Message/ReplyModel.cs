using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.User;

namespace PPgram.MVVM.Models.Message;

partial class ReplyModel : ObservableObject
{
    [ObservableProperty]
    private ProfileModel sender = new();
    [ObservableProperty]
    private string text = string.Empty;
}
