using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Dialog;

internal partial class RegularDialog : Dialog
{
    [ObservableProperty]
    private string header = string.Empty;
    [ObservableProperty]
    private string text = string.Empty;
    [ObservableProperty]
    private string accept = string.Empty;
    [ObservableProperty]
    private string decline = string.Empty;
    [ObservableProperty]
    private DialogIcons icon;

    [RelayCommand]
    private void CloseDialog()
    {
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
        WeakReferenceMessenger.Default.Send(new Msg_RegularDialogResult { action = "cancel" });
    }
}
public enum DialogIcons
{
    None,
    Error,
    Check,
    Info,
    Notify
}