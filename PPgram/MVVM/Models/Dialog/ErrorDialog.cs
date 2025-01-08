using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.Dialog;

internal partial class ErrorDialog : Dialog
{
    [ObservableProperty]
    private string text = string.Empty;
    public ErrorDialog()
    {
        Position = VerticalAlignment.Top;
        backpanel = false;
    }
}
