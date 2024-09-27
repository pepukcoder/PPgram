using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models;
using PPgram.MVVM.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace PPgram.MVVM.ViewModels;

partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    #region pages
    private readonly RegViewModel reg_vm = new();
    private readonly LoginViewModel login_vm = new();
    private readonly ChatViewModel chat_vm = new();
    #endregion

    public MainViewModel() 
    {
        // events
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, options) => ShowDialog(options));

        CurrentPage = login_vm;
    }
}