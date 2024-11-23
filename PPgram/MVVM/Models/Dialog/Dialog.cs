using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.Dialog;

internal abstract partial class Dialog : ObservableObject
{
    [ObservableProperty]
    private VerticalAlignment position = VerticalAlignment.Center;
    public bool backpanel = true;
    public bool canSkip = true;
}
