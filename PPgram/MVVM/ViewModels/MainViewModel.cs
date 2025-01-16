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
using PPgram.MVVM.Models.User;
using PPgram.Net;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
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

    private readonly DispatcherTimer timer = new();
    private readonly ProfileState profileState = ProfileState.Instance;
    public MainViewModel()
    {
        // ui
        WeakReferenceMessenger.Default.Register<Msg_ShowDialog>(this, (r, m) => ShowDialog(m.dialog, m.time));
        WeakReferenceMessenger.Default.Register<Msg_CloseDialog>(this, (r, m) => { Dialog = null; DialogPanelVisible = false; });
        WeakReferenceMessenger.Default.Register<Msg_OpenPreviewer>(this, (r, m) => MediaPreviewer.Open(m.content, m.file));
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
        // network
        WeakReferenceMessenger.Default.Register<Msg_Reconnect>(this, async (r, m) =>
        {
            jsonClient.Disconnect();
            filesClient.Disconnect();
            try
            {
                await jsonClient.Connect(PPAppState.ConnectionOptions);
                await filesClient.Connect(PPAppState.ConnectionOptions);
                await AutoAuth();
                await LoadOnline();
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
                await LoadOffline();
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_Auth>(this, async (r, m) =>
        {
            AuthDTO auth;
            try
            {
                if (!String.IsNullOrEmpty(m.name)) auth = await jsonClient.Register(m.username, m.name, m.password);
                else auth = await jsonClient.Login(m.username, m.password);
                FSManager.CreateFile(PPPath.SessionFile, JsonSerializer.Serialize(auth));
                await LoadOnline();
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
                await LoadOffline();
            }
        });
        // api
        WeakReferenceMessenger.Default.Register<Msg_CheckUsername>(this, async (r, m) =>
        {
            try
            {
                bool available = await jsonClient.CheckUsername(m.username);
                reg_vm.ShowUsernameStatus(available ? "Username is available" : "Username is already taken", available);
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_SearchUsers>(this, async (r, m) =>
        {
            try
            {
                List<ChatDTO> dtos = await jsonClient.FetchUsers(m.query);
                ObservableCollection<ChatModel> results = [];
                foreach (ChatDTO dto in dtos)
                {
                    ChatModel model = ConvertChat(dto);
                    model.Searched = true;
                    results.Add(model);
                }
                chat_vm.UpdateSearch(results);
            }
            catch
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = "Failed to fetch search results" }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_FetchMessages>(this, async (r, m) =>
        {
            try
            {
                if (m.anchor.Id == 0 || m.anchor.Id == -1) return;
                int[] range;
                if (m.forward) range = [m.anchor.Id + 1, PPAppState.MessagesFetchAmount - 1];
                else range = [m.anchor.Id - 1, (PPAppState.MessagesFetchAmount - 1) * -1];
                List<MessageDTO> messageDTOs = await jsonClient.FetchMessages(m.anchor.Chat, range);
                List<MessageModel> messages = [];
                foreach (MessageDTO messageDTO in messageDTOs) messages.Add(await ConvertMessage(messageDTO));
                // write method in chatmodel for upper and lower appending with rechaining
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_SendMessage>(this, async (r, m) =>
        {
            try
            {
                // move chat from search to chatlist if new
                chat_vm.AddChatIfNotExists(m.to);
                // get message text if set
                string text;
                if (m.message.Content is ITextContent tc) text = tc.Text;
                else text = "";
                // upload files first if attached
                List<string> hashes = [];
                if (m.message.Content is FileContentModel fc)
                {
                    foreach (FileModel file in fc.Files)
                    {
                        if (file is VideoModel video) file.Hash = await filesClient.UploadFile(video.Path, true, false);
                        else if (file is PhotoModel photo) file.Hash = await filesClient.UploadFile(photo.Path, true, photo.Compress);
                        else file.Hash = await filesClient.UploadFile(file.Path, false, false);
                        if (file.Hash != null)
                        {
                            hashes.Add(file.Hash);
                            file.Status = FileStatus.Loaded;
                            // auto cache file
                        }
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
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessage>(this, async (r, m) =>
        {
            try
            {
                //await jsonClient.DeleteMessage(m.chat, m.Id);
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessage>(this, async (r, m) =>
        {
            try
            {
                string text;
                if (m.newContent is ITextContent textContent) text = textContent.Text;
                else text = "";
                await jsonClient.EditMessage(m.chat,m.Id, text);
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_SendDraft>(this, async (r, m) =>
        {
            await jsonClient.SendDraft(m.chat_id, m.draft);
        });
        WeakReferenceMessenger.Default.Register<Msg_SendRead>(this, async (r, m) =>
        {
            try
            {
                int[] msg_ids = m.messages.Select(message => message.Id).ToArray();
                await jsonClient.SendRead(m.messages.First().Chat, msg_ids);
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_CreateGroup>(this, async (r, m) =>
        {
            try 
            { 
                ChatDTO chatDTO = await jsonClient.CreateGroup(m.name, m.username, String.Empty);
                chat_vm.Chats.Add(ConvertChat(chatDTO));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_DownloadFile>(this, async (r, m) =>
        {
            try
            {
                if (m.file.Hash == null) throw new InvalidOperationException("Download file failed");
                (string? preview_temp, string? file_temp) = await filesClient.DownloadFile(m.file.Hash, DownloadMode.media_only);
                if (file_temp != null)
                {
                    CacheManager.CacheFile(m.file.Hash, m.file.Name, preview_temp, file_temp);
                    string file_path = CacheManager.GetCachedFile(m.file.Hash) ?? throw new SQLiteException("Load cached file failed");
                    m.file.Path = file_path;
                    m.file.Status = FileStatus.Loaded;
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
            }
        });
        // chat events
        WeakReferenceMessenger.Default.Register<Msg_NewChatEvent>(this, (r, m) =>
        {
            if (m.chat != null) chat_vm.Chats.Add(ConvertChat(m.chat));
        });
        WeakReferenceMessenger.Default.Register<Msg_NewMessageEvent>(this, async (r, m) =>
        {
            if (m.message != null) chat_vm.AddMessage(await ConvertMessage(m.message));        
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessageEvent>(this, async (r, m) =>
        {
            if (m.message != null) chat_vm.EditMessage(await ConvertMessage(m.message));
        });
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessageEvent>(this, (r, m) =>
        {
            if (m.chat != -1 || m.id != -1) chat_vm.DeleteMessage(m.chat, m.id);
        });
        WeakReferenceMessenger.Default.Register<Msg_MarkAsReadEvent>(this, (r, m) =>
        {
            if (m.chat != -1) foreach (int id in m.ids) chat_vm.ChangeMessageStatus(m.chat, id, MessageStatus.Read);
        });
        WeakReferenceMessenger.Default.Register<Msg_IsTypingEvent>(this, (r, m) =>
        {
            if (m.chat == -1 || m.user == -1) return;
            if (m.typing) chat_vm.ChangeChatStatus(m.chat, ChatStatus.Typing);
            else chat_vm.ChangeChatStatus(m.chat, ChatStatus.None);
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
        try
        {
            CurrentPage = chat_vm;
            ProfileDTO self = await jsonClient.FetchSelf();
            profileState.UserId = self.Id ?? 0;
            profileState.Name = self.Name ?? string.Empty;
            profileState.Username = self.Username ?? string.Empty;
            profileState.Avatar = Base64ToBitmapConverter.ConvertBase64(self.Photo);
            List<ChatDTO> chatDTOs = await jsonClient.FetchChats();
            foreach (ChatDTO chatDTO in chatDTOs)
            {
                ChatModel chat = ConvertChat(chatDTO);
                chat_vm.Chats.Add(chat);
                List<MessageDTO> messageDTOs = await jsonClient.FetchMessages(chat.Id, [-1, -99]);
                List<MessageModel> messages = [];
                foreach (MessageDTO messageDTO in messageDTOs) messages.Add(await ConvertMessage(messageDTO));
                chat_vm.LoadMessages(chat.Id, messages);
            }
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
        }
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
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = "Auto authentification failed" }, time = 3 });
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
    private async Task<MessageModel> ConvertMessage(MessageDTO messageDTO)
    {
        MessageContentModel content;
        if (messageDTO.MediaHashes != null && messageDTO.MediaHashes.Length != 0)
        {
            ObservableCollection<FileModel> files = [];
            foreach (string hash in messageDTO.MediaHashes)
            {
                try
                {
                    (string name, long size) = await filesClient.DownloadFileMetadata(hash);
                    // assign model based on file extension
                    string extension = Path.GetExtension(name);
                    if (PPFileExtensions.VideoExtensions.Contains(extension))
                    {
                        VideoModel video = new()
                        {
                            Name = name,
                            Size = size,
                            Hash = hash,
                        };
                        if (!CacheManager.IsCached(hash))
                        {
                            (string? temp_preview, string? temp_file) = await filesClient.DownloadFile(hash, DownloadMode.preview_only);
                            CacheManager.CacheFile(hash, name, temp_preview, temp_file);
                        }
                        string? preview_path = CacheManager.GetCachedFile(hash, true);
                        string? file_path = CacheManager.GetCachedFile(hash);
                        if (preview_path != null) video.Preview = new(preview_path);
                        if (file_path != null)
                        {
                            video.Path = new(file_path);
                            video.Status = FileStatus.Loaded;
                        }
                        files.Add(video);
                    }
                    else if (PPFileExtensions.PhotoExtensions.Contains(extension))
                    {
                        PhotoModel photo = new()
                        {
                            Name = name,
                            Size = size,
                            Hash = hash,
                        };
                        if (!CacheManager.IsCached(hash))
                        {
                            (string? temp_preview, string? temp_file) = await filesClient.DownloadFile(hash, DownloadMode.preview_only);
                            CacheManager.CacheFile(hash, name, temp_preview, temp_file);
                        }
                        string? preview_path = CacheManager.GetCachedFile(hash, true);
                        string? file_path = CacheManager.GetCachedFile(hash);
                        if (preview_path != null) photo.Preview = new(preview_path);
                        if (file_path != null)
                        {
                            photo.Path = new(file_path);
                            photo.Status = FileStatus.Loaded;
                        }
                        files.Add(photo);
                    }
                    else
                    {
                        FileModel file = new()
                        {
                            Name = name,
                            Size = size,
                            Hash = hash,
                        };
                        string? file_path = CacheManager.GetCachedFile(hash);
                        if (file_path != null)
                        {
                            file.Path = new(file_path);
                            file.Status = FileStatus.Loaded;
                        }
                        files.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new ErrorDialog { Text = ex.Message }, time = 3 });
                }
            }
            content = new FileContentModel()
            {
                Files = files,
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
        return new MessageModel
        {
            Id = messageDTO.Id ?? 0,
            Chat = messageDTO.ChatId ?? 0,
            SenderId = messageDTO.From ?? 0,
            Time = messageDTO.Date ?? 0,
            ReplyTo = messageDTO.ReplyTo ?? 0,
            Edited = messageDTO.Edited ?? false,
            Content = content,
            Status = messageDTO.Unread == false ? MessageStatus.Read : MessageStatus.Delivered
        };
    }
    private static ChatModel ConvertChat(ChatDTO chatDTO)
    {
        ProfileModel profile = new()
        {
            Name = chatDTO.Name ?? string.Empty,
            Username = chatDTO.Username ?? string.Empty,
            Avatar = Base64ToBitmapConverter.ConvertBase64(chatDTO.Photo)
        };
        if (chatDTO.IsGroup == true)
        {
            return new GroupModel()
            {
                Id = chatDTO.Id ?? 0,
                Profile = profile,
            };
        }
        else
        {
            return new UserModel()
            {
                Id = chatDTO.Id ?? 0,
                Profile = profile,
            };
        }
    }
    private void ShowDialog(Dialog dialog, int time)
    {
        Dialog = dialog;
        DialogPanelVisible = dialog.backpanel;
        DialogPosition = dialog.Position;
        if (time != 0)
        {
            timer.Interval = TimeSpan.FromSeconds(time);
            timer.Tick += CloseDialogTimer;
            timer.Start();
        }
    }
    private void CloseDialogTimer(object? sender, EventArgs e) => CloseDialog();
    [RelayCommand]
    private void CloseDialog()
    {
        if (!Dialog?.canSkip == true) return;
        timer.Stop();
        Dialog = null;
        DialogPanelVisible = false;
    }
}
