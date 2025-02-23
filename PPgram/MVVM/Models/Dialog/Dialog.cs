using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Dialog;

internal partial class Dialog : ObservableObject
{
    [ObservableProperty]
    public partial DialogIcons Icon { get; set; }
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;
    [ObservableProperty]
    public partial VerticalAlignment Position { get; set; } = VerticalAlignment.Center;
    public bool backpanel = true;
    public bool canSkip = true;
}
