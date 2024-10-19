using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.User;
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
    #region path
    // folders
    private static readonly string localAppPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string basePath = Path.Combine(localAppPath, "PPgram");
    private static readonly string cachePath = Path.Combine(basePath, "cache");
    private static readonly string settingsPath = Path.Combine(basePath, "settings");
    // files
    private static readonly string sessionFilePath = Path.Combine(basePath, "session.sesf");
    private static readonly string connectionFilePath = Path.Combine(settingsPath, "connection.setf");
    private static readonly string appSettingsFilePath = Path.Combine(settingsPath, "app.setf");
    #endregion
    private readonly Client client = new();
    private ProfileState profileState = ProfileState.Instance;
    public MainViewModel() 
    {
        // events 
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, options) => ShowDialog(options));
        WeakReferenceMessenger.Default.Register<Msg_CheckResult>(this, (r, e) =>
            reg_vm.ShowUsernameStatus(e.available 
            ? "Username is available"
            : "Username is already taken",
            e.available)
        );
        WeakReferenceMessenger.Default.Register<Msg_Login>(this, (r, e) => 
        {
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                icon = DialogIcons.Error,
                header = "Connection error",
                text = sessionFilePath,
            });
            client.AuthLogin(e.username, e.password);
        });
        WeakReferenceMessenger.Default.Register<Msg_Register>(this, (r, e) => 
        {
            if (e.check) client.ChekUsername(e.username);
            else client.RegisterUser(e.username, e.name, e.password);
        });
        WeakReferenceMessenger.Default.Register<Msg_AuthResult>(this, (r, e) => 
        {
            if(!e.auto) CreateFile(sessionFilePath, e.sessionId + Environment.NewLine + e.userId);
            CurrentPage = chat_vm;
            client.FetchSelf();
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchSelfResult>(this, (r, e) => 
        {
            profileState.UserId = e.profile?.Id ?? 0;
            profileState.Name = e.profile?.Name ?? string.Empty;
            profileState.Username = e.profile?.Username ?? string.Empty;
            profileState.Avatar = Base64ToBitmapConverter.ConvertBase64(e.profile?.Photo);
            client.FetchChats();
        });
        WeakReferenceMessenger.Default.Register <Msg_FetchChatsResult>(this, (r, e) =>
        {
            ObservableCollection<ChatModel> chats = [];
        });
        // connection
        CurrentPage = login_vm;
        ConnectToServer();   
    }
    private static void CreateFile(string path, string data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        using var writer = new StreamWriter(File.OpenWrite(path));
        writer.Write(data);
    }
    private void ConnectToServer()
    {
        string host;
        int port;
        if(!File.Exists(connectionFilePath)) CreateFile(connectionFilePath, "127.0.0.1" + Environment.NewLine + "3000");
        try
        {
            string[] lines = File.ReadAllLines(connectionFilePath);
            host = lines[0];
            port = int.Parse(lines[1]);
        }
        catch
        {
            File.Delete(connectionFilePath);
            host = "127.0.0.1";
            port = 3000;
        }
        client.Connect(host, port);
        if (!File.Exists(sessionFilePath)) return;
        try
        {
            string[] lines = File.ReadAllLines(sessionFilePath);
            string session_id = lines[0];
            int user_id = Int32.Parse(lines[1]);
            client.AuthSessionId(session_id, user_id);
        }
        catch
        {
            File.Delete(sessionFilePath);
        } 
    }
}