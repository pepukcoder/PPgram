using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.User;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace PPgram.Net;

internal class JsonClient
{
    private TcpClient? client;
    private NetworkStream? stream;
    private ConnectionOptions? options;
    private CancellationTokenSource cts = new();
    private readonly AppState state = AppState.Instance;
    private readonly ConcurrentDictionary<int, object> requests = [];
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private int requestId = 0;
    private bool reconnecting;
    public async Task<bool> Connect(ConnectionOptions connectionOptions)
    {
        try
        {
            client = new();
            options = connectionOptions;
            Task task = client.ConnectAsync(options.JsonHost, options.JsonPort);
            if (await Task.WhenAny(task, Task.Delay(5000)) != task) throw new TimeoutException("Connection to server timed out");
            stream = client.GetStream();
            _ = Task.Run(() => Listen(cts.Token));
            return true;
        }
        catch { return false; }
    }
    /// <summary>
    /// Listens for server response in a loop, handles recieved response in dedicated task
    /// </summary>
    private async Task Listen(CancellationToken ct)
    {
        TcpConnection connection = new();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                connection.ReadStream(stream);
                if (connection.IsReady)
                {
                    string response = connection.GetResponseAsString();
                    HandleResponse(response);
                }
            }
            catch { Debug.WriteLine("[JSON] listen failed"); await Disconnect(); }
        }
    }
    /// <summary>
    /// Disconnects from server and resets client socket and requests
    /// </summary>
    public async Task Disconnect()
    {
        if (reconnecting) return;
        if (client?.Connected == true) client.Close();
        requests.Clear();
        cts.Cancel();
        cts = new();
        if (options == null || state.ReconnectionDelay < 0) return;
        reconnecting = true;
        while (reconnecting)
        {
            await Task.Delay(state.ReconnectionDelay);
            Debug.WriteLine("[JSON] reconnect attempt");
            if (await Connect(options))
            {
                AuthDTO credentials = await FSManager.LoadFromJsonFile<AuthDTO>(PPPath.SessionFile);
                if (await AuthSession(credentials.SessionId ?? string.Empty, credentials.UserId ?? 0))
                {
                    Debug.WriteLine("[JSON] reconnect success");
                    reconnecting = false;
                    break;
                }
            }
            Debug.WriteLine("[JSON] reconnect fail");
        }
    }
    /// <summary>
    /// Sends serialized json to server
    /// </summary>
    /// <param name="data">Serializable request payload</param>
    /// <returns></returns>
    private async Task Send(object data, object tcs)
    {
        if (stream == null) return;
        await semaphore.WaitAsync();
        try
        {
            if (!requests.TryAdd(requestId, tcs)) return;
            string request = JsonSerializer.Serialize(data);
            byte[] payload = TcpConnection.BuildJsonRequest(request);
            await stream.WriteAsync(payload);
            Debug.WriteLine($"[JSON] Sent: {requestId}");
        }
        catch
        {
            Debug.WriteLine($"[JSON] Send failed: {requestId}");
            requests.TryRemove(requestId, out _);
            await Disconnect();
        }
        finally
        {
            requestId++;
            semaphore.Release();
        }
    }
    /// <summary>
    /// Sends serialized json to server and enqueues tcs into request queue
    /// </summary>
    /// <param name="data">Serializable request payload</param>
    /// <param name="tcs">Completition source</param>
    /// <returns></returns>
    public async Task<bool> AuthSession(string session_id, int user_id)
    {
        var payload = new
        {
            req_id = requestId,
            method = "auth",
            user_id,
            session_id,
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<AuthDTO> AuthLogin(string username, string password)
    {
        var payload = new
        {
            req_id = requestId,
            method = "login",
            username,
            password
        };
        TaskCompletionSource<AuthDTO> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<AuthDTO> AuthRegister(string username, string name, string password)
    {
        var payload = new
        {
            req_id = requestId,
            method = "register",
            username,
            name,
            password
        };
        TaskCompletionSource<AuthDTO> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> CheckUsername(string username)
    {
        var payload = new
        {
            req_id = requestId,
            method = "check",
            what = "username",
            data = username
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> EditSelf(ProfileModel profile)
    {
        var payload = new
        {
            req_id = requestId,
            method = "edit",
            what = "self",
            name = profile.Name,
            photo = profile.Avatar.Hash,
            profile_color = profile.Color,
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<ProfileDTO> FetchSelf()
    {
        var payload = new
        {
            req_id = requestId,
            method = "fetch",
            what = "self"
        };
        TaskCompletionSource<ProfileDTO> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<List<ChatDTO>> FetchChats()
    {
        var payload = new
        {
            req_id = requestId,
            method = "fetch",
            what = "chats"
        };
        TaskCompletionSource<List<ChatDTO>> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<List<ChatDTO>> FetchUsers(string query)
    {
        var payload = new
        {
            req_id = requestId,
            method = "fetch",
            what = "users",
            query
        };
        TaskCompletionSource<List<ChatDTO>> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<List<MessageDTO>> FetchMessages(int id, int[] range)
    {
        var payload = new
        {
            req_id = requestId,
            method = "fetch",
            what = "messages",
            chat_id = id,
            range
        };
        TaskCompletionSource<List<MessageDTO>> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<(int, int)> SendMessage(int to, int? reply_to, string text, List<string> hashes)
    {
        var payload = new
        {
            req_id = requestId,
            method = "send_message",
            reply_to = reply_to != null ? reply_to : null,
            content = new
            {
                text,
                sha256_hashes = hashes.ToArray(),
            },
            to
        };
        TaskCompletionSource<(int, int)> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> EditMessage(int chat_id, int message_id, string new_text)
    {
        var payload = new
        {
            req_id = requestId,
            method = "edit",
            what = "message",
            content = new_text,
            chat_id,
            message_id
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> DeleteMessage(int chat_id, int message_id)
    {
        var payload = new
        {
            req_id = requestId,
            method = "delete",
            what = "messages",
            chat_id,
            message_ids = new int[] { message_id }
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> SendDraft(int chat_id, string draft)
    {
        var payload = new
        {
            req_id = requestId,
            method = "edit",
            what = "draft",
            chat_id,
            draft
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<int> ReadMessage(int chat_id, int[] message_ids)
    {
        var payload = new
        {
            req_id = requestId,
            method = "edit",
            what = "is_unread",
            chat_id,
            message_ids
        };
        TaskCompletionSource<int> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<ChatDTO> CreateGroup(string name, string? username, string? avatar_hash)
    {
        var payload = new
        {
            req_id = requestId,
            method = "new",
            what = "group",
            name,
            username,
            avatar_hash
        };
        TaskCompletionSource<ChatDTO> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> DeleteChat(int chat_id)
    {
        var payload = new
        {
            req_id = requestId,
            method = "delete",
            what = "chat",
            chat_id
        };
        TaskCompletionSource<bool> tcs = new();
        await Send(payload, tcs);
        return await tcs.Task;
    }
    private void HandleResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();
        int? r_id = rootNode?["req_id"]?.GetValue<int>();
        // parse specific fields
        if (r_method != null && r_id.HasValue)
        {
            Debug.WriteLine($"[JSON] got: {r_id.Value}");
            switch (r_method)
            {
                case "login":
                case "register":
                    if (requests.TryRemove(r_id.Value, out object? tcs) && tcs is TaskCompletionSource<AuthDTO> login_tcs)
                    {
                        AuthDTO? auth = rootNode?.Deserialize<AuthDTO>();
                        if (ok == true && auth != null) login_tcs.SetResult(auth);
                        else login_tcs.SetException(new Exception(r_error ?? "Authentification failed"));
                    }
                    break;
                case "auth":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<bool> auth_tcs)
                    {
                        if (ok != null) auth_tcs.SetResult(ok == true);
                        else auth_tcs.SetException(new Exception(r_error ?? "Auto authentification failed"));
                    }
                    break;
                case "check_username":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<bool> check_tcs)
                    {
                        if (ok != null) check_tcs.SetResult(ok != true);
                        else check_tcs.SetException(new Exception(r_error ?? "Check username failed"));
                    }
                    break;
                case "fetch_self":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<ProfileDTO> fself_tcs)
                    {
                        ProfileDTO? profile = rootNode?.Deserialize<ProfileDTO>();
                        if (ok == true && profile != null) fself_tcs.SetResult(profile);
                        else fself_tcs.SetException(new Exception(r_error ?? "Fetch profile failed"));
                    }
                    break;
                case "fetch_chats":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<List<ChatDTO>> fchats_tcs)
                    {
                        JsonArray? chatsJson = rootNode?["chats"]?.AsArray();
                        if (ok == true && chatsJson != null)
                        {
                            List<ChatDTO> chatlist = [];
                            foreach (JsonNode? chatNode in chatsJson)
                            {
                                ChatDTO? chat = chatNode?.Deserialize<ChatDTO>();
                                if (chat != null) chatlist.Add(chat);
                            }
                            fchats_tcs.SetResult(chatlist);
                        }
                        else fchats_tcs.SetException(new Exception(r_error ?? "Fetch chats failed"));
                    }
                    break;
                case "fetch_users":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<List<ChatDTO>> fusers_tcs)
                    {
                        JsonArray? usersJson = rootNode?["users"]?.AsArray();
                        if (ok == true && usersJson != null)
                        {
                            List<ChatDTO> userList = [];
                            foreach (JsonNode? userNode in usersJson)
                            {
                                ChatDTO? user = userNode?.Deserialize<ChatDTO>();
                                if (user != null)
                                {
                                    user.Id = userNode?["user_id"]?.GetValue<int>() ?? -1;
                                    userList.Add(user);
                                }
                            };
                            fusers_tcs.SetResult(userList);
                        }
                        else fusers_tcs.SetException(new Exception(r_error ?? "Fetch users failed"));
                    }
                    break;
                case "fetch_messages":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<List<MessageDTO>> fmsg_tcs)
                    {
                        JsonArray? messagesJson = rootNode?["messages"]?.AsArray();
                        if (ok == true && messagesJson != null)
                        {
                            List<MessageDTO> messageList = [];
                            foreach (JsonNode? messageNode in messagesJson)
                            {
                                MessageDTO? message = messageNode?.Deserialize<MessageDTO>();
                                if (message != null) messageList.Add(message);
                            }
                            fmsg_tcs.SetResult(messageList);
                        }
                        else fmsg_tcs.SetException(new Exception(r_error ?? "Fetch messages failed"));
                    }
                    break;
                case "send_message":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<(int, int)> msg_tcs)
                    {
                        int? messageId = rootNode?["message_id"]?.GetValue<int>();
                        int? chatId = rootNode?["chat_id"]?.GetValue<int>();
                        if (ok == true && messageId != null && chatId != null) msg_tcs.SetResult((messageId ?? -1, chatId ?? -1));
                        else msg_tcs.SetException(new Exception(r_error ?? "Send message failed"));
                    }
                    break;
                case "delete_message":
                    requests.Remove(r_id.Value, out _);
                    break;
                case "edit_draft":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<bool> editdraft_tcs)
                    {
                        if (ok != null) editdraft_tcs.SetResult(ok == true);
                        else editdraft_tcs.SetException(new Exception(r_error ?? "Send draft failed"));
                    }
                    break;
                case "edit_is_unread":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<int> editread_tcs)
                    {
                        int? chatId = rootNode?["chat_id"]?.GetValue<int>();
                        if (ok == true && chatId != null) editread_tcs.SetResult(chatId ?? -1);
                        else editread_tcs.SetException(new Exception(r_error ?? "Read message failed"));
                    }
                    break;
                case "edit_self":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<bool> editself_tcs)
                    {
                        if (ok == true) editself_tcs.SetResult(true);
                        else editself_tcs.SetException(new Exception(r_error ?? "Edit profile failed"));
                    }
                    break;
                case "new_group":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<ChatDTO> newgroup_tcs)
                    {
                        JsonNode? chatNode = rootNode?["chat"];
                        ChatDTO? group = chatNode.Deserialize<ChatDTO>();
                        if (ok == true && group != null)
                        {
                            group.Username = chatNode?["tag"]?.GetValue<string>();
                            newgroup_tcs.SetResult(group);
                        }
                        else newgroup_tcs.SetException(new Exception(r_error ?? "Create group failed"));
                    }
                    break;
                case "delete_chat":
                    if (requests.TryRemove(r_id.Value, out tcs) && tcs is TaskCompletionSource<bool> deletechat_tcs)
                    {
                        if (ok != null) deletechat_tcs.SetResult(ok == true);
                        else deletechat_tcs.SetException(new Exception(r_error ?? "Delete chat failed"));
                    }
                    break;
            }
            Debug.WriteLine($"[JSON] left: {requests.Count}");
        }
        // parse events
        if (r_event != null)
        {
            switch (r_event)
            {
                case "new_message":
                    JsonNode? messageNode = rootNode?["new_message"];
                    if (messageNode == null) return;
                    MessageDTO? messageDTO = messageNode.Deserialize<MessageDTO>();
                    WeakReferenceMessenger.Default.Send(new Msg_NewMessageEvent { chat = messageDTO?.ChatId ?? -1, message = messageDTO });
                    break;
                case "edit_message":
                    messageNode = messageNode = rootNode?["new_message"];
                    if (messageNode == null) return;
                    messageDTO = messageNode.Deserialize<MessageDTO>();
                    WeakReferenceMessenger.Default.Send(new Msg_EditMessageEvent { chat = messageDTO?.ChatId ?? -1, message = messageDTO });
                    break;
                case "delete_message":
                    int? chat = rootNode?["chat_id"]?.GetValue<int>();
                    int? id = rootNode?["message_id"]?.GetValue<int>();
                    if (chat == null || id == null) return;
                    WeakReferenceMessenger.Default.Send(new Msg_DeleteMessageEvent { chat = chat ?? -1, id = id ?? -1 });
                    break;
                case "new_chat":
                    JsonNode? chatNode = rootNode?["new_chat"];
                    if (chatNode == null) return;
                    ChatDTO? chatDTO = chatNode.Deserialize<ChatDTO>();
                    WeakReferenceMessenger.Default.Send(new Msg_NewChatEvent { chat = chatDTO });
                    break;
                case "is_typing":
                    bool? typing = rootNode?["is_typing"]?.GetValue<bool>();
                    chat = rootNode?["chat_id"]?.GetValue<int>();
                    int? user = rootNode?["user_id"]?.GetValue<int>();
                    if (chat == null || user == null || typing == null) return;
                    WeakReferenceMessenger.Default.Send(new Msg_IsTypingEvent { typing = typing ?? false, chat = chat ?? -1, user = user ?? -1 });
                    break;
                case "mark_as_read":
                    chat = rootNode?["chat_id"]?.GetValue<int>();
                    JsonArray? idsJson = rootNode?["message_ids"]?.AsArray();
                    if (chat == null || idsJson == null) return;
                    List<int> ids = [];
                    foreach (JsonNode? idJson in idsJson)
                    {
                        id = idJson?.GetValue<int>();
                        ids.Add(id ?? -1);
                    }
                    WeakReferenceMessenger.Default.Send(new Msg_MarkAsReadEvent { chat = chat ?? -1, ids = [.. ids] });
                    break;
                case "edit_self":
                    ProfileDTO? profile = rootNode?["new_profile"]?.Deserialize<ProfileDTO>();
                    if (profile == null ) return;
                    WeakReferenceMessenger.Default.Send(new Msg_EditProfileEvent { profile = profile });
                    break;                    
            }
        }
    }
}
