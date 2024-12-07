using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Net;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace PPgram.MVVM.ViewModels;

partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;
    [ObservableProperty]
    private Dialog? dialog;
    [ObservableProperty]
    private bool dialogPanelVisible = false;
    [ObservableProperty]
    private VerticalAlignment dialogPosition = VerticalAlignment.Center;
    #region pages
    private readonly RegViewModel reg_vm = new();
    private readonly LoginViewModel login_vm = new();
    private readonly ChatViewModel chat_vm = new();
    private readonly ProfileViewModel profile_vm = new();
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
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, e) => ShowDialog(e.dialog));
        WeakReferenceMessenger.Default.Register<Msg_CloseDialog>(this, (r, e) => { Dialog = null; DialogPanelVisible = false; });
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_Reconnect>(this, (r,e) => ConnectToServer());
        WeakReferenceMessenger.Default.Register<Msg_SearchChats>(this, (r, e) => jsonClient.SearchChats(e.searchQuery));
        WeakReferenceMessenger.Default.Register<Msg_FetchMessages>(this, (r, e) => jsonClient.FetchMessages(e.chatId, e.range));
        WeakReferenceMessenger.Default.Register<Msg_Login>(this, (r, e) => jsonClient.AuthLogin(e.username, e.password));
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessage>(this, (r, e) => jsonClient.DeleteMessage(e.chat, e.Id));
        WeakReferenceMessenger.Default.Register<Msg_UploadFiles>(this, (r, e) =>
        {
            Thread thread = new(() => UploadFiles(e.files)) { IsBackground = true };
            thread.Start();
        });
        WeakReferenceMessenger.Default.Register<Msg_Register>(this, (r, e) =>
        {
            if (e.check) jsonClient.CheckUsername(e.username);
            else jsonClient.RegisterUser(e.username, e.name, e.password);
        });
        WeakReferenceMessenger.Default.Register<Msg_AuthResult>(this, (r, e) =>
        {
            AuthCredentialsModel data = new()
            {
                UserId = e.userId,
                SessionId = e.sessionId
            };
            JsonSerializerOptions options = new() { WriteIndented = true }; // Pretty print the JSON
            if (!e.auto) FSManager.CreateFile(sessionFilePath, JsonSerializer.Serialize(data));
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
        WeakReferenceMessenger.Default.Register<Msg_SendMessage>(this, (r, e) =>
        {
            string text;
            List<string> hashes = [];
            if (e.message.Content is ITextContent tc) text = tc.Text;
            else text = "";

            if (e.message.Content is FileContentModel fc)
            {
                foreach (FileModel file in fc.Files)
                {
                    if (file.Hash != null) hashes.Add(file.Hash);
                }
            }
            jsonClient.SendMessage(e.to, e.message.ReplyTo, text, hashes);
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessage>(this, (r, e) =>
        {
            string text;
            if (e.newContent is ITextContent textContent) text = textContent.Text;
            else text = "";
            jsonClient.EditMessage(e.chat,e.Id, text);
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessageEvent>(this, (r, e) =>
        {
            if (e.message == null) return;
            chat_vm.EditMessage(DTOToModelConverter.ConvertMessage(e.message));
        });

        // connection
        CurrentPage = login_vm;
        ConnectToServer();
    }
    
    private void ConnectToServer()
    {
        ConnectionOptions connectionOptions = new()
        {
            Host = "127.0.0.1",
            JsonPort = 3000,
            FilesPort = 8080
        };
        if(!File.Exists(connectionFilePath)) FSManager.CreateFile(connectionFilePath, JsonSerializer.Serialize(connectionOptions));
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
    private void UploadFiles(ObservableCollection<FileModel> files)
    {
        try
        {
            foreach (FileModel file in files)
            {
                file.Hash = filesClient.UploadFile(file.Path);
            }
            WeakReferenceMessenger.Default.Send(new Msg_UploadFilesResult { ok = true });
        }
        catch
        {
            WeakReferenceMessenger.Default.Send(new Msg_UploadFilesResult { ok = false });
        }
    }
    [RelayCommand]
    private void ShowDialog(Dialog dialog)
    {
        Dialog = dialog;
        DialogPanelVisible = dialog.backpanel;
        DialogPosition = dialog.Position;
    }
    [RelayCommand]
    private void CloseDialog()
    {
        if (!Dialog?.canSkip == true) return;
        Dialog = null;
        DialogPanelVisible = false;
    }
}
