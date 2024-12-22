using Avalonia.Layout;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Media;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Net;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PPgram.MVVM.ViewModels;

internal partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentPage;
    [ObservableProperty]
    private Dialog? dialog;
    [ObservableProperty]
    private bool dialogPanelVisible = false;
    [ObservableProperty]
    private VerticalAlignment dialogPosition = VerticalAlignment.Center;
    [ObservableProperty]
    private MediaPreviewer mediaPreviewer = new();
    [ObservableProperty]
    private AppState pPAppState = AppState.Instance;
    // pages
    private readonly RegViewModel reg_vm = new();
    private readonly LoginViewModel login_vm = new();
    private readonly ChatViewModel chat_vm = new();
    private readonly ProfileViewModel profile_vm = new();
    // network
    private readonly JsonClient jsonClient = new();
    private readonly FilesClient filesClient = new();
    
    private readonly ProfileState profileState = ProfileState.Instance;
    public MainViewModel()
    {
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, e) => ShowDialog(e.dialog));
        WeakReferenceMessenger.Default.Register<Msg_CloseDialog>(this, (r, e) => { Dialog = null; DialogPanelVisible = false; });
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, e) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, e) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_Logout>(this, (r, e) => Logout());
        WeakReferenceMessenger.Default.Register<Msg_Reconnect>(this, async (r, m) =>
        {
            if(await ConnectToServer()) await AutoAuth();
        });

        WeakReferenceMessenger.Default.Register<Msg_Auth>(this, async (r, m) =>
        {
            AuthDTO auth;
            if (String.IsNullOrEmpty(m.name)) auth = await jsonClient.Register(m.username, m.name, m.password);
            else auth = await jsonClient.Login(m.username, m.password);
            FSManager.CreateFile(PPPath.SessionFile, JsonSerializer.Serialize(auth));
            await LoadOnline();
        });
        WeakReferenceMessenger.Default.Register<Msg_CheckUsername>(this, async (r, m) =>
        {
            bool available = await jsonClient.CheckUsername(m.username);
            reg_vm.ShowUsernameStatus(available ? "Username is available" : "Username is already taken", available);
        });
        WeakReferenceMessenger.Default.Register<Msg_SearchChats>(this, async (r, e) =>
        {
            ////////////////
            jsonClient.SearchChats(e.searchQuery);
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchMessages>(this, async (r, m) =>
        {
            List<MessageDTO> dtos = await jsonClient.FetchMessages(m.chatId, m.range);
            List<MessageModel> messages = [];
            foreach (MessageDTO dto in dtos) messages.Add(DTOToModelConverter.ConvertMessage(dto));
            chat_vm.UpdateMessages(m.chatId, messages);
        });
        
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessage>(this, (r, e) => jsonClient.DeleteMessage(e.chat, e.Id));
        WeakReferenceMessenger.Default.Register<Msg_UploadFiles>(this, (r, e) =>
        {
            Thread thread = new(() => UploadFiles(e.files)) { IsBackground = true };
            thread.Start();
        });
        WeakReferenceMessenger.Default.Register<Msg_SearchChatsResult>(this, (r, e) =>
        {
            ObservableCollection<ChatModel> resultList = [];
            foreach (ChatDTO chat in e.users)
            {
                if (chat.Id == profileState.UserId) continue;
                UserModel result = new()
                {
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
            //chat_vm.UpdateSearch(resultList);
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
        WeakReferenceMessenger.Default.Register<Msg_DownloadFile>(this, (r, e) =>
        {
            if (e.file.Hash == null) return;
            if (e.meta)
            {
                MetadataModel? meta = filesClient.DownloadMetadata(e.file.Hash);
                e.file.Name = meta?.FileName ?? "???";
                e.file.Size = meta?.FileSize ?? 0;
            }
            else
            {
                filesClient.DownloadFiles(e.file.Hash);
                e.file.Status = FileStatus.Loaded;
            }
        });

        // connection
        CurrentPage = login_vm;
        Task.Run(async() => 
        {
            if (await ConnectToServer() && await AutoAuth()) await LoadOnline();
        });
    }
    private async Task LoadOnline()
    {
        CurrentPage = chat_vm;
        /*
        profileState.UserId = e.profile?.Id ?? 0;
        profileState.Name = e.profile?.Name ?? string.Empty;
        profileState.Username = e.profile?.Username ?? string.Empty;
        profileState.Avatar = Base64ToBitmapConverter.ConvertBase64(e.profile?.Photo);
        jsonClient.FetchChats();*/
    }
    private async Task<bool> AutoAuth()
    {
        if (!File.Exists(PPPath.SessionFile)) return false;
        try
        {
            string json = File.ReadAllText(PPPath.SessionFile);
            AuthDTO? credentials = JsonSerializer.Deserialize<AuthDTO>(json) ?? throw new InvalidDataException();
            return await jsonClient.Auth(credentials.SessionId ?? string.Empty, credentials.UserId ?? 0);
        }
        catch
        {
            File.Delete(PPPath.SessionFile);
            return false;
        }
    }
    private void Logout()
    {
        File.Delete(PPPath.SessionFile);
        CurrentPage = login_vm;
        // TODO: make api call to end auth session
    }
    private async Task<bool> ConnectToServer()
    {
        ConnectionOptions options = new()
        {
            Host = "127.0.0.1",
            JsonPort = 3000,
            FilesPort = 8080
        };
        // try load settings
        if (!File.Exists(PPPath.ConnectionFile)) FSManager.CreateFile(PPPath.ConnectionFile, JsonSerializer.Serialize(options));
        try
        {
            string data = File.ReadAllText(PPPath.ConnectionFile);
            options = JsonSerializer.Deserialize<ConnectionOptions>(data) ?? throw new InvalidDataException();
        }
        catch { File.Delete(PPPath.ConnectionFile); }
        return await jsonClient.Connect(options) && await filesClient.Connect(options);
    }
    private void UploadFiles(ObservableCollection<FileModel> files)
    {
        try
        {
            foreach (FileModel file in files)
            {
                file.Hash = filesClient.UploadFile(file.Path);
                if (file.Hash != null)
                {
                    file.Status = FileStatus.Loaded;
                    FSManager.SaveBinary(file.Hash, File.ReadAllBytes(file.Path), file.Name, false);
                }
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
