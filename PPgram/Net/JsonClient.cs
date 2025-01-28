using CommunityToolkit.Mvvm.Messaging;
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
    private CancellationTokenSource cts = new();
    private readonly ConcurrentQueue<object> requests = [];
    private readonly SemaphoreSlim semaphore = new(1, 1);
    public async Task<bool> Connect(ConnectionOptions options)
    {
        try
        {
            if (client != null) throw new InvalidOperationException("Client is already connected");
            client = new();
            Task task = client.ConnectAsync(options.Host, options.JsonPort);
            if (await Task.WhenAny(task, Task.Delay(5000)) != task) throw new TimeoutException("Connection to server timed out");
            stream = client.GetStream();
            Thread listenThread = new(() => Listen(cts.Token)) { IsBackground = true };
            listenThread.Start();
            return true;
        }
        catch { return false; }
    }
    /// <summary>
    /// Listens for server response in a loop, handles recieved response in dedicated task
    /// </summary>
    private void Listen(CancellationToken ct)
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
                    Task.Run(() => HandleResponse(response), ct);
                }
            }
            catch { Disconnect(); }
        }
    }
    /// <summary>
    /// Disconnects from server and resets client socket and requests
    /// </summary>
    public void Disconnect()
    {
        if (client?.Client.Connected == true) client?.Client.Disconnect(false);
        cts.Cancel();
        requests.Clear();
        client = null;
        cts = new();
    }
    /// <summary>
    /// Sends serialized json to server
    /// </summary>
    /// <param name="data">Serializable request payload</param>
    /// <returns></returns>
    private async Task Send(object data)
    {
        semaphore.Wait();
        try
        {
            string request = JsonSerializer.Serialize(data);
            if (stream == null) return;
            await stream.WriteAsync(TcpConnection.BuildJsonRequest(request));
        }
        catch { Disconnect(); }
        finally { semaphore.Release(); }
    }
    /// <summary>
    /// Sends serialized json to server and enqueues tcs into request queue
    /// </summary>
    /// <param name="data">Serializable request payload</param>
    /// <param name="tcs"></param>
    /// <returns></returns>
    private async Task SendQueue(object data, object tcs)
    {
        if (stream == null) return;
        requests.Enqueue(tcs);
        await Send(data);
    }
    public async Task<bool> Auth(string session_id, int user_id)
    {
        var payload = new
        {
            method = "auth",
            user_id,
            session_id,
        };
        TaskCompletionSource<bool> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<AuthDTO> Login(string username, string password)
    {
        var payload = new
        {
            method = "login",
            username,
            password
        };
        TaskCompletionSource<AuthDTO> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<AuthDTO> Register(string username, string name, string password)
    {
        var payload = new
        {
            method = "register",
            username,
            name,
            password
        };
        TaskCompletionSource<AuthDTO> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> CheckUsername(string username)
    {
        var payload = new
        {
            method = "check",
            what = "username",
            data = username
        };
        TaskCompletionSource<bool> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<ProfileDTO> FetchSelf()
    {
        var payload = new
        {
            method = "fetch",
            what = "self"
        };
        TaskCompletionSource<ProfileDTO> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<List<ChatDTO>> FetchChats()
    {
        var payload = new
        {
            method = "fetch",
            what = "chats"
        };
        TaskCompletionSource<List<ChatDTO>> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<List<ChatDTO>> FetchUsers(string query)
    {
        var payload = new
        {
            method = "fetch",
            what = "users",
            query
        };
        TaskCompletionSource<List<ChatDTO>> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<List<MessageDTO>> FetchMessages(int id, int[] range)
    {
        var payload = new
        {
            method = "fetch",
            what = "messages",
            chat_id = id,
            range
        };
        TaskCompletionSource<List<MessageDTO>> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<(int, int)> SendMessage(int to, int? reply_to, string text, List<string> hashes)
    {
        var payload = new
        {
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
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> EditMessage(int chat_id, int message_id, string new_text)
    {
        var payload = new
        {
            method = "edit",
            what = "message",
            content = new_text,
            chat_id,
            message_id
        };
        TaskCompletionSource<bool> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> DeleteMessage(int chat_id, int message_id)
    {
        var payload = new
        {
            method = "delete",
            what = "messages",
            chat_id,
            message_ids = new int[] { message_id }
        };
        TaskCompletionSource<bool> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> SendDraft(int chat_id, string draft)
    {
        var payload = new
        {
            method = "edit",
            what = "draft",
            chat_id,
            draft
        };
        TaskCompletionSource<bool> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<int> SendRead(int chat_id, int[] message_ids)
    {
        var payload = new
        {
            method = "edit",
            what = "is_unread",
            chat_id,
            message_ids
        };
        TaskCompletionSource<int> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<ChatDTO> CreateGroup(string name, string username, string avatar_hash)
    {
        var payload = new
        {
            method = "new",
            what = "group",
            name,
            username,
            avatar_hash
        };
        TaskCompletionSource<ChatDTO> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> DeleteChat(int chat_id)
    {
        var payload = new
        {
            method = "delete",
            what = "chat",
            chat_id
        };
        TaskCompletionSource<bool> tcs = new();
        await SendQueue(payload, tcs);
        return await tcs.Task;
    }
    private void HandleResponse(string response)
    {
        Debug.WriteLine(response);
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();
        // parse specific fields
        if (r_method != null)
        {
            Debug.WriteLine($"before: {requests.Count}");
            switch (r_method)
            {
                case "login":
                case "register":
                    requests.TryDequeue(out object? tcs);
                    if (tcs is TaskCompletionSource<AuthDTO> login_tcs)
                    {
                        AuthDTO? auth = rootNode?.Deserialize<AuthDTO>();
                        if (ok == true && auth != null) login_tcs.SetResult(auth);
                        else login_tcs.SetException(new Exception(r_error ?? "Authentification failed"));
                    }
                    break;
                case "auth":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<bool> auth_tcs)
                    {
                        if (ok != null) auth_tcs.SetResult(ok == true);
                        else auth_tcs.SetException(new Exception(r_error ?? "Auto authentification failed"));
                    }
                    break;
                case "check_username":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<bool> check_tcs)
                    {
                        if (ok != null) check_tcs.SetResult(ok != true);
                        else check_tcs.SetException(new Exception(r_error ?? "Check username failed"));
                    }
                    break;
                case "fetch_self":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<ProfileDTO> fself_tcs)
                    {
                        ProfileDTO? profile = rootNode?.Deserialize<ProfileDTO>();
                        if (ok == true && profile != null) fself_tcs.SetResult(profile);
                        else fself_tcs.SetException(new Exception(r_error ?? "Fetch profile failed"));
                    }
                    break;
                case "fetch_chats":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<List<ChatDTO>> fchats_tcs)
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
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<List<ChatDTO>> fusers_tcs)
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
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<List<MessageDTO>> fmsg_tcs)
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
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<(int, int)> msg_tcs)
                    {
                        int? messageId = rootNode?["message_id"]?.GetValue<int>();
                        int? chatId = rootNode?["chat_id"]?.GetValue<int>();
                        if (ok == true && messageId != null && chatId != null) msg_tcs.SetResult((messageId ?? -1, chatId ?? -1));
                        else msg_tcs.SetException(new Exception(r_error ?? "Send message failed"));
                    }
                    break;
                case "edit_draft":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<bool> editdraft_tcs)
                    {
                        if (ok != null) editdraft_tcs.SetResult(ok == true);
                        else editdraft_tcs.SetException(new Exception(r_error ?? "Send draft failed"));
                    }
                    break;
                case "edit_is_unread":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<int> editread_tcs)
                    {
                        int? chatId = rootNode?["chat_id"]?.GetValue<int>();
                        if (ok == true && chatId != null) editread_tcs.SetResult(chatId ?? -1);
                        else editread_tcs.SetException(new Exception(r_error ?? "Read message failed"));
                    }
                    break;
                case "new_group":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<ChatDTO> newgroup_tcs)
                    {
                        ChatDTO? group = rootNode?["chat"]?.Deserialize<ChatDTO>();
                        if (ok == true && group != null) newgroup_tcs.SetResult(group);
                        else newgroup_tcs.SetException(new Exception(r_error ?? "Create group failed"));
                    }
                    break;
                case "delete_chat":
                    requests.TryDequeue(out tcs);
                    if (tcs is TaskCompletionSource<bool> deletechat_tcs)
                    {
                        if (ok != null) deletechat_tcs.SetResult(ok == true);
                        else deletechat_tcs.SetException(new Exception(r_error ?? "Delete chat failed"));
                    }
                    break;
            }
            Debug.WriteLine($"after: {requests.Count}");
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
            }
        }
    }
}
