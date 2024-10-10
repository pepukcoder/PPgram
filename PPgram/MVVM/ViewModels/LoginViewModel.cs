using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;

namespace PPgram.MVVM.ViewModels;

partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _username = string.Empty;
    [ObservableProperty]
    private string _password = string.Empty;

    [RelayCommand]
    private void TryLogin()
    {
        // check if all fields are filled
        if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Password)) return;
        WeakReferenceMessenger.Default.Send(new Msg_Login
        {
            username = Username,
            password = Password
        });
        // remove this LOL
        WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
        {
            header = "Trying to login huh?",
            text = "you just a clown, use terminal",
            accept = "yes you are right"
        });
    }
    [RelayCommand]
    private void ToRegPage()
    {
        // reset password field
        Password = "";
        WeakReferenceMessenger.Default.Send<Msg_ToReg>();
    }
}
class Msg_ToReg;
class Msg_Login
{
    public string username = string.Empty;
    public string password = string.Empty;
}
