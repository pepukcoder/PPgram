
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Net;
using PPgram.Shared;

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

    private readonly Client client = new();
    public MainViewModel() 
    {
        // before-connection events 
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, options) => ShowDialog(options));

        CurrentPage = login_vm;
        client.Connect("127.0.0.1", 8080);

        // after connection events
        WeakReferenceMessenger.Default.Register<Msg_CheckResult>(this, (r, e) =>
            reg_vm.ShowUsernameStatus(e.available 
            ? "This username is available"
            : "This username is already taken",
            e.available)
        );
        WeakReferenceMessenger.Default.Register<Msg_Login>(this, (r, e) => 
        {
            client.AuthLogin(e.username, e.password);
        });
        WeakReferenceMessenger.Default.Register<Msg_Register>(this, (r, e) => 
        {
            if (e.check) client.ChekUsername(e.username);
            else client.RegisterUser(e.username, e.name, e.password);
        });
        WeakReferenceMessenger.Default.Register<Msg_AuthResult>(this, (r, e) => 
        {
            if(!e.auto)
            {
                // save session and user ids to app files
            }
            // show chat page
            CurrentPage = chat_vm;
        });
    }
}