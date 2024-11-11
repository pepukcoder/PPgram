using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.User;
using PPgram.Net;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

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
    private readonly JsonClient jsonClient = new();
    private readonly FilesClient filesClient = new();
    private readonly ProfileState profileState = ProfileState.Instance;
    public MainViewModel() 
    {
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, options) => ShowDialog(options));
        WeakReferenceMessenger.Default.Register<Msg_SearchChats>(this, (r, e) => { jsonClient.SearchChats(e.searchQuery); });
        WeakReferenceMessenger.Default.Register<Msg_SendMessage>(this, (r, e) => { jsonClient.SendMessage(e.message, e.to); });
        WeakReferenceMessenger.Default.Register<Msg_FetchMessages>(this, (r, e) => { jsonClient.FetchMessages(e.chatId, e.range); });
        WeakReferenceMessenger.Default.Register<Msg_Login>(this, (r, e) => { jsonClient.AuthLogin(e.username, e.password); });
        WeakReferenceMessenger.Default.Register<Msg_CheckResult>(this, (r, e) =>
            reg_vm.ShowUsernameStatus(e.available 
            ? "Username is available"
            : "Username is already taken",
            e.available)
        );
        WeakReferenceMessenger.Default.Register<Msg_Register>(this, (r, e) => 
        {
            if (e.check) jsonClient.CheckUsername(e.username);
            else jsonClient.RegisterUser(e.username, e.name, e.password);
        });
        WeakReferenceMessenger.Default.Register<Msg_AuthResult>(this, (r, e) => 
        {
            var data = new AuthCredentialsModel
            {
                UserId = e.userId,
                SessionId = e.sessionId
            };
            var options = new JsonSerializerOptions { WriteIndented = true }; // Pretty print the JSON
            if(!e.auto) CreateFile(sessionFilePath, JsonSerializer.Serialize(data));
            CurrentPage = chat_vm;
            jsonClient.FetchSelf();
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchSelfResult>(this, (r, e) => 
        {
            profileState.UserId = e.profile?.Id ?? 0;
            profileState.Name = e.profile?.Name ?? string.Empty;
            profileState.Username = e.profile?.Username ?? string.Empty;
            profileState.Avatar = Base64ToBitmapConverter.ConvertBase64(e.profile?.Photo);
            jsonClient.FetchChats();
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchChatsResult>(this, (r, e) =>
        {
            ObservableCollection<ChatModel> chats = [];
            foreach (ChatDTO chat in e.chats)
            {
                chats.Add(DTOToModelConverter.ConvertChat(chat));
            }
            chat_vm.UpdateChats(chats);
        });
        WeakReferenceMessenger.Default.Register<Msg_SearchChatsResult>(this, (r, e) =>
        {
            ObservableCollection<ChatModel> resultList = [];
            foreach (ChatDTO chat in e.users)
            {
                if (chat.Id == profileState.UserId) continue;
                UserModel result = new()
                {
                    Type = ChatType.Chat,
                    Id = chat.Id ?? 0,
                    Profile = new() 
                    {
                        Name = chat.Name ?? "",
                        Username = chat.Username ?? "",
                        Avatar = Base64ToBitmapConverter.ConvertBase64(chat.Photo)
                    }
                };
                resultList.Add(result);
            }
            chat_vm.UpdateSearch(resultList);
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchMessagesResult>(this, (r, e) =>
        {
            ObservableCollection<MessageModel> messages = [];
            foreach (MessageDTO messageDTO in e.messages)
            {
                messages.Add(DTOToModelConverter.ConvertMessage(messageDTO));
            }
            chat_vm.UpdateMessages(messages);
        });
        WeakReferenceMessenger.Default.Register<Msg_NewChat>(this, (r, e) =>
        {
            if (e.chat == null) return;
            chat_vm.AddChat(DTOToModelConverter.ConvertChat(e.chat));
        });
        WeakReferenceMessenger.Default.Register<Msg_NewMessage>(this, (r, e) =>
        {
            if (e.message == null) return;
            chat_vm.AddMessage(DTOToModelConverter.ConvertMessage(e.message));
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
        ConnectionOptions connectionOptions = new()
        {
            Host = "127.0.0.1",
            JsonPort = 3000,
            FilesPort = 8080
        };
        if(!File.Exists(connectionFilePath)) CreateFile(connectionFilePath, JsonSerializer.Serialize(connectionOptions));
        try
        {
            string data = File.ReadAllText(connectionFilePath);
            connectionOptions = JsonSerializer.Deserialize<ConnectionOptions>(data) ?? throw new Exception();
        }
        catch
        {
            File.Delete(connectionFilePath);
        }
        jsonClient.Connect(connectionOptions.Host, connectionOptions.JsonPort);
        filesClient.Connect(connectionOptions.Host, connectionOptions.FilesPort);

        if (!File.Exists(sessionFilePath)) return;
        try
        {
            string json = File.ReadAllText(sessionFilePath);
            AuthCredentialsModel? creds = JsonSerializer.Deserialize<AuthCredentialsModel>(json) ?? throw new Exception();
            jsonClient.AuthSessionId(creds.SessionId, creds.UserId);
        }
        catch
        {
            File.Delete(sessionFilePath);
        } 
    }
}
