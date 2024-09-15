using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;

namespace PPgram.MVVM.ViewModels;

partial class RegViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;
    [ObservableProperty]
    private string _username;
    [ObservableProperty]
    private string _password;
    [ObservableProperty]
    private string _passwordConf;
    [ObservableProperty]
    private string _usernameStatus;
    [ObservableProperty]
    private bool _usernameOk;
    [ObservableProperty]
    private bool _passOk;
    [ObservableProperty]
    private bool _passConfOk;

    private readonly DispatcherTimer _timer;
    public RegViewModel()
    {
        // username check request delay timer
        _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += CheckUsername;
    }
    partial void OnPasswordChanged(string value)
    {
        // check if password length is valid
        if (value.Length >= 8 && value.Length <= 28) PassOk = true;
        else PassOk = false;
        OnPasswordConfChanged(PasswordConf);
    }
    partial void OnPasswordConfChanged(string value)
    {
        // check if password confirmation matches password
        if (value == Password) PassConfOk = true;
        else PassConfOk = false;
    }
    partial void OnUsernameChanged(string value)
    {
        UsernameOk = false;
        // stop timer if user is still editing username
        _timer.Stop();
        if (!String.IsNullOrEmpty(value))
        {
            // check if username is valid characters
            foreach (char c in value)
            {
                if (!Char.IsAsciiLetterOrDigit(c) && c != '_' || value.StartsWith('_'))
                {
                    ShowUsernameStatus("Invalid username");
                    return;
                }
            }
            // check username length
            if (value.Length < 3)
            {
                ShowUsernameStatus("Username is too short");
                return;
            }
            // restart delay if username is valid
            _timer.Start();
        }
        ShowUsernameStatus();
    }
    private void CheckUsername(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        _timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_Register()
        {
            username = $"@{Username}",
            check = true
        });
    }
    public void ShowUsernameStatus(string status = "", bool ok = false)
    {
        UsernameStatus = status;
        UsernameOk = ok;
    }

    [RelayCommand]
    private void TryRegister()
    {
        // check if all fields are valid
        if (String.IsNullOrEmpty(Name) || !UsernameOk || !PassOk || !PassConfOk) return;
        WeakReferenceMessenger.Default.Send(new Msg_Register()
        {
            name = Name,
            username = Username,
            password = Password
        });
    }
    [RelayCommand]
    private void ToLoginPage()
    {
        // reset password fields
        Password = "";
        PasswordConf = "";
        PassOk = false;
        PassConfOk = false;
        WeakReferenceMessenger.Default.Send<Msg_ToLogin>();
    }
}
class Msg_ToLogin;
class Msg_Register
{
    public string name = "";
    public string username = "";
    public string password = "";
    public bool check = false;
}