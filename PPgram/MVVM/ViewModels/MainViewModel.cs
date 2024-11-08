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
using System.Diagnostics;
using System.Text.Json;
using PPgram.Net.DTO;
using PPgram.MVVM.Models.Message;
using System.Linq;
using PPgram.MVVM.Models.MessageContent;
using System.Collections.Generic;
using PPgram.MVVM.Models.File;

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
        WeakReferenceMessenger.Default.Register<Msg_SearchChats>(this, (r, e) => { client.SearchChats(e.searchQuery); });
        WeakReferenceMessenger.Default.Register<Msg_SendMessage>(this, (r, e) => { client.SendMessage(e.message, e.to); });
        WeakReferenceMessenger.Default.Register<Msg_FetchMessages>(this, (r, e) => { client.FetchMessages(e.chatId, e.range); });
        WeakReferenceMessenger.Default.Register<Msg_Login>(this, (r, e) => { client.AuthLogin(e.username, e.password); });
        WeakReferenceMessenger.Default.Register<Msg_CheckResult>(this, (r, e) =>
            reg_vm.ShowUsernameStatus(e.available 
            ? "Username is available"
            : "Username is already taken",
            e.available)
        );
        WeakReferenceMessenger.Default.Register<Msg_Register>(this, (r, e) => 
        {
            if (e.check) client.ChekUsername(e.username);
            else client.RegisterUser(e.username, e.name, e.password);
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
        WeakReferenceMessenger.Default.Register<Msg_FetchChatsResult>(this, (r, e) =>
        {
            ObservableCollection<ChatModel> chats = [];
            foreach (ChatDTO chat in e.chats)
            {
                ProfileModel profile = new()
                {
                    Name = chat.Name ?? string.Empty,
                    Username = chat.Username ?? string.Empty,
                    Avatar = Base64ToBitmapConverter.ConvertBase64(chat.Photo)
                };
                if (chat.IsGroup == true)
                {
                    GroupModel group = new()
                    {
                        Id = chat.Id ?? 0,
                        Type = ChatType.Group,
                        Profile = profile,
                    };
                    chats.Add(group);
                }
                else
                {
                    UserModel user = new()
                    {
                        Id = chat.Id ?? 0,
                        Type = ChatType.Chat,
                        Profile = profile,
                    };
                    chats.Add(user);
                }
            }
            chat_vm.UpdateChats(chats);
        });
        WeakReferenceMessenger.Default.Register<Msg_SearchChatsResult>(this, (r, e) =>
        {
            ObservableCollection<SearchEntryModel> resultList = [];
            foreach (ProfileDTO chat in e.users)
            {
                SearchEntryModel result = new()
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
                MessageContentModel content;
                if (messageDTO.MediaHashes != null && messageDTO.MediaHashes.Length != 0)
                {
                    List<FileModel> files = [];
                    foreach (string hash in messageDTO.MediaHashes)
                    {
                        files.Add(new() { Hash = hash });
                    }
                    content = new FileContentModel()
                    {
                        Files = new(files),
                        Text = messageDTO.Text ?? string.Empty
                    };
                }
                else
                {
                    content = new TextContentModel()
                    {
                        Text = messageDTO.Text ?? string.Empty
                    };
                }
                MessageModel messageModel = new()
                {
                    Id = messageDTO.Id ?? 0,
                    Chat = messageDTO.ChatId ?? 0,
                    SenderId = messageDTO.From ?? 0,
                    Time = messageDTO.Date ?? 0,
                    ReplyTo = messageDTO.ReplyTo ?? 0,
                    Content = content,
                    Status = messageDTO.Unread == false ? MessageStatus.Read : MessageStatus.Delivered
                };
                messages.Add(messageModel);
            }
            chat_vm.UpdateMessages(messages);
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
            string json = File.ReadAllText(sessionFilePath);
            AuthCredentialsModel? creds = JsonSerializer.Deserialize<AuthCredentialsModel>(json) ?? throw new Exception();
            client.AuthSessionId(creds.SessionId, creds.UserId);
        }
        catch
        {
            File.Delete(sessionFilePath);
        } 
    }
}
