using System;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Dialog;

internal partial class ConnectionDialog : Dialog
{
    [RelayCommand]
    private static void Reconnect()
    {
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
        WeakReferenceMessenger.Default.Send(new Msg_Reconnect());
    }
}
