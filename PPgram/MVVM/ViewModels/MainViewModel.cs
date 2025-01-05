using Avalonia.Layout;
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
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, m) => ShowDialog(m.dialog));
        WeakReferenceMessenger.Default.Register<Msg_CloseDialog>(this, (r, m) => { Dialog = null; DialogPanelVisible = false; });
        WeakReferenceMessenger.Default.Register<Msg_ToLogin>(this, (r, m) => CurrentPage = login_vm);
        WeakReferenceMessenger.Default.Register<Msg_ToReg>(this, (r, m) => CurrentPage = reg_vm);
        WeakReferenceMessenger.Default.Register<Msg_Logout>(this, (r, m) =>
        {
            string? exeFile = Process.GetCurrentProcess().MainModule?.FileName;
            if (exeFile == null) return;
            File.Delete(PPPath.SessionFile);
            Process.Start(new ProcessStartInfo(exeFile));
            Environment.Exit(0);
        });
        WeakReferenceMessenger.Default.Register<Msg_Reconnect>(this, async (r, m) =>
        {
            jsonClient.Disconnect();
            filesClient.Disconnect();
            await jsonClient.Connect(PPAppState.ConnectionOptions);
            await filesClient.Connect(PPAppState.ConnectionOptions);
            await AutoAuth();
            await LoadOnline();
        });
        WeakReferenceMessenger.Default.Register<Msg_Auth>(this, async (r, m) =>
        {
            AuthDTO auth;
            if (!String.IsNullOrEmpty(m.name)) auth = await jsonClient.Register(m.username, m.name, m.password);
            else auth = await jsonClient.Login(m.username, m.password);
            FSManager.CreateFile(PPPath.SessionFile, JsonSerializer.Serialize(auth));
            await LoadOnline();
        });
        WeakReferenceMessenger.Default.Register<Msg_CheckUsername>(this, async (r, m) =>
        {
            bool available = await jsonClient.CheckUsername(m.username);
            reg_vm.ShowUsernameStatus(available ? "Username is available" : "Username is already taken", available);
        });
        WeakReferenceMessenger.Default.Register<Msg_SearchUsers>(this, async (r, m) =>
        {
            List<ChatDTO> dtos = await jsonClient.FetchUsers(m.query);
            ObservableCollection<ChatModel> results = [];
            foreach (ChatDTO dto in dtos)
            {
                ChatModel model = DTOToModelConverter.ConvertChat(dto);
                model.Searched = true;
                results.Add(model);
            }
            chat_vm.UpdateSearch(results);
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchMessages>(this, async (r, m) =>
        {
            List<MessageDTO> dtos = await jsonClient.FetchMessages(m.chatId, m.range);
            chat_vm.LoadMessages(m.chatId, dtos);
        });
        WeakReferenceMessenger.Default.Register<Msg_SendMessage>(this, async (r, m) =>
        {
            string text;
            List<string> hashes = [];
            // get message text if set
            if (m.message.Content is ITextContent tc) text = tc.Text;
            else text = "";

            // upload files first if attached
            if (m.message.Content is FileContentModel fc)
            {
                foreach (FileModel file in fc.Files)
                {
                    // implement file sending
                    if (file.Hash != null) hashes.Add(file.Hash);
                }
            }
            // get id from response and assign status
            (int, int) response = await jsonClient.SendMessage(m.to.Id, m.message.ReplyTo, text, hashes);
            if (m.to.Id == 0) m.to.Id = response.Item2;
            if (response.Item1 != -1 && response.Item2 != -1)
            {
                m.message.Id = response.Item1;
                chat_vm.ChangeMessageStatus(m.to.Id, m.message.Id, MessageStatus.Delivered);
            }
            else
            {
                chat_vm.ChangeMessageStatus(m.to.Id, m.message.Id, MessageStatus.Error);
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessage>(this, async (r, m) =>
        {
            await jsonClient.DeleteMessage(m.chat, m.Id);
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessage>(this, async (r, m) =>
        {
            string text;
            if (m.newContent is ITextContent textContent) text = textContent.Text;
            else text = "";
            await jsonClient.EditMessage(m.chat,m.Id, text);
        });
        WeakReferenceMessenger.Default.Register<Msg_SendDraft>(this, async (r, m) =>
        {
            await jsonClient.SendDraft(m.chat_id, m.draft);
        });
        WeakReferenceMessenger.Default.Register<Msg_SendRead>(this, async (r, m) =>
        {
            int[] msg_ids = m.messages.Select(message => message.Id).ToArray();
            await jsonClient.SendRead(m.messages.First().Chat, msg_ids);
        });
        WeakReferenceMessenger.Default.Register<Msg_CreateGroup>(this, async (r, m) =>
        {
            ChatDTO chatDTO = await jsonClient.CreateGroup(m.name, m.username, String.Empty);
            chat_vm.Chats.Add(DTOToModelConverter.ConvertChat(chatDTO));
        });
        WeakReferenceMessenger.Default.Register<Msg_UploadFile>(this, async (r, m) => 
        {
            // TODO: Modify AttachFilesDialog to send file type and compression method 
        });

        // connection
        CurrentPage = login_vm;
        Task.Run(async() =>
        {
            if (await ConnectToServer() && await AutoAuth()) await LoadOnline();
            else await LoadOffline();
        });
    }
    private async Task LoadOnline()
    {
        CurrentPage = chat_vm;
        ProfileDTO self = await jsonClient.FetchSelf();
        profileState.UserId = self.Id ?? 0;
        profileState.Name = self.Name ?? string.Empty;
        profileState.Username = self.Username ?? string.Empty;
        profileState.Avatar = Base64ToBitmapConverter.ConvertBase64(self.Photo);
        List<ChatDTO> chatDTOs = await jsonClient.FetchChats();
        foreach (ChatDTO dto in chatDTOs)
        {
            ChatModel chat = DTOToModelConverter.ConvertChat(dto);
            chat_vm.Chats.Add(chat);
            List<MessageDTO> messages = await jsonClient.FetchMessages(chat.Id, [-1, -99]);
            chat_vm.LoadMessages(chat.Id, messages);
        }
        /* DEBUG file sending
        string hash = await filesClient.UploadFile("path\\to\\file", true, false);
        (string? preview_temp, string? file_temp) = await filesClient.DownloadFile(hash, DownloadMode.full);
        Debug.WriteLine($"preview path: {preview_temp}");
        Debug.WriteLine($"file path: {file_temp}");
        */
    }
    private async Task LoadOffline()
    {
        // Placeholder for offline app use (probably in future???)
    }
    private async Task<bool> AutoAuth()
    {
        if (!File.Exists(PPPath.SessionFile)) return false;
        try
        {
            AuthDTO credentials = FSManager.LoadFromJsonFile<AuthDTO>(PPPath.SessionFile);
            return await jsonClient.Auth(credentials.SessionId ?? string.Empty, credentials.UserId ?? 0);
        }
        catch
        {
            File.Delete(PPPath.SessionFile);
            return false;
        }
    }
    private async Task<bool> ConnectToServer()
    {
        // try load settings
        if (!File.Exists(PPPath.ConnectionFile)) FSManager.CreateJsonFile(PPPath.ConnectionFile, PPAppState.ConnectionOptions);
        try { PPAppState.ConnectionOptions = FSManager.LoadFromJsonFile<ConnectionOptions>(PPPath.ConnectionFile); }
        catch { File.Delete(PPPath.ConnectionFile); }
        return await jsonClient.Connect(PPAppState.ConnectionOptions) && await filesClient.Connect(PPAppState.ConnectionOptions);
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
