using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Views;
using System;
using System.Diagnostics;

namespace PPgram.MVVM.ViewModels;

partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    #region pages
    private readonly RegViewModel reg_vm = new();
    private readonly LoginViewModel login_vm = new();
    #endregion

    public MainViewModel() 
    {
        CurrentPage = login_vm;

        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, options) => ShowDialog(options));
        DialogTestPavlo();
    }
    private void DialogTestPavlo()
    {
        WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
        {
            text = "Here is very important question" + Environment.NewLine + "Are you pavlo?",
            header = "PAVLO ALERT",
            icon = DialogIcons.Info,
            accept = "Yeah",
            decline = "Hell nah"
        });
        WeakReferenceMessenger.Default.Register<Msg_DialogResult>(this, (r, result) =>
        {
            WeakReferenceMessenger.Default.Unregister<Msg_DialogResult>(this);
            if (result.action == DialogAction.Accepted)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                {
                    text = "pavlo confirmed :)",
                    decline = ""
                });
            }
        });
    }
}