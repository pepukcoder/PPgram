﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using PPgram.Shared;

namespace PPgram.MVVM.ViewModels;

partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _dialogText = string.Empty;
    [ObservableProperty]
    private string _dialogHeader = string.Empty;
    [ObservableProperty]
    private DialogIcons _dialogIcon = DialogIcons.None;
    [ObservableProperty]
    private string _dialogAccept = "Ok";
    [ObservableProperty]
    private string _dialogDecline = "Cancel";
    
    private void ShowDialog(Msg_ShowDialog options)
    {
        // check to prevent empty call
        if (!string.IsNullOrEmpty(options.text.Trim()))
        {
            // show dialog with given options
            DialogText = options.text.Trim();
            DialogHeader = options.header.Trim();
            DialogIcon = options.icon;
            DialogAccept = options.accept.Trim();
            DialogDecline = options.decline.Trim();
        }
    }
    [RelayCommand]
    private void CloseDialog(string parameter)
    {
        // reset content
        DialogText = DialogHeader = string.Empty;
        DialogIcon = DialogIcons.None;
        // reset buttons
        DialogAccept = DialogDecline = string.Empty;
        // parse action dialog was closed with
        if (Enum.TryParse(parameter, true, out DialogAction dialogAction))
        {
            // send result to subscriber
            WeakReferenceMessenger.Default.Send(new Msg_DialogResult
            {
                action = dialogAction
            });
        }
    }
}