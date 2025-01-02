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
    private readonly ConcurrentQueue<object> requests = [];
    private CancellationTokenSource cts = new();
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
    private void Listen(CancellationToken ct)
    {
        JsonConnection connection = new();
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
    public void Disconnect()
    {
        if (client?.Client.Connected == true) client?.Client.Disconnect(false);
        cts.Cancel();
        requests.Clear();
        client = null;
        cts = new();
    }
    private void Send(object data, object tcs)
    {
        try
        {
            requests.Enqueue(tcs);
            string request = JsonSerializer.Serialize(data);
            stream?.Write(JsonConnection.BuildJsonRequest(request));
        }
        catch { Disconnect(); }
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
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
        Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<bool> DeleteMessage(int chat_id, int message_id)
    {
        var payload = new
        {
            method = "delete",
            chat_id,
            message_id
        };
        TaskCompletionSource<bool> tcs = new();
        Send(payload, tcs);
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
        Send(payload, tcs);
        return await tcs.Task;
    }
    public async Task<int> SendRead(int chat_id, int message_id)
    {
        var payload = new
        {
            method = "edit",
            what = "is_unread",
            chat_id,
            message_id
        };
        TaskCompletionSource<int> tcs = new();
        Send(payload, tcs);
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

        #if DEBUG
        Debug.WriteLine(response);
        if (ok == false && r_method != null && r_error != null) {
            Debug.WriteLine($"[DEBUG] Error in method: {r_method}\n[DEBUG] Error:{r_error}");
            return;
        }
        #endif

        // parse specific fields
        if (r_method != null && requests.TryDequeue(out object? tcs))
        switch (r_method)
        {
            case "login":
            case "register":
                if (ok != true) return;
                AuthDTO? auth = rootNode?.Deserialize<AuthDTO>();
                if (auth == null) return;
                if (tcs is TaskCompletionSource<AuthDTO> login_tcs) login_tcs.SetResult(auth);
                break;
            case "auth":
                if (ok != true) return;
                if (tcs is TaskCompletionSource<bool> auth_tcs) auth_tcs.SetResult(true);
                break;
            case "check_username":
                if (ok != true && ok != false) return;
                if (tcs is TaskCompletionSource<bool> check_tcs) check_tcs.SetResult(ok != true);
                break;
            case "fetch_self":
                if (ok != true) return;
                ProfileDTO? profile = rootNode?.Deserialize<ProfileDTO>();
                if (profile == null) return;
                if (tcs is TaskCompletionSource<ProfileDTO> fself_tcs) fself_tcs.SetResult(profile);
                break;
            case "fetch_chats":
                if (ok != true) return;
                JsonArray? chatsJson = rootNode?["chats"]?.AsArray();
                if (chatsJson == null) return;
                List<ChatDTO> chatlist = [];
                foreach (JsonNode? chatNode in chatsJson)
                {
                    ChatDTO? chat = chatNode?.Deserialize<ChatDTO>();
                    if (chat != null) chatlist.Add(chat);
                }
                if (tcs is TaskCompletionSource<List<ChatDTO>> fchats_tcs) fchats_tcs.SetResult(chatlist);
                break;
            case "fetch_users":
                if (ok != true) return;
                JsonArray? usersJson = rootNode?["users"]?.AsArray();
                if (usersJson == null) return;
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
                if (tcs is TaskCompletionSource<List<ChatDTO>> fusers_tcs) fusers_tcs.SetResult(userList);
                break;
            case "fetch_messages":
                if (ok != true) return;
                JsonArray? messagesJson = rootNode?["messages"]?.AsArray();
                if (messagesJson == null) return;
                List<MessageDTO> messageList = [];
                foreach(JsonNode? messageNode in messagesJson)
                {
                    MessageDTO? message = messageNode?.Deserialize<MessageDTO>();
                    if (message != null) messageList.Add(message);
                }
                messageList.Reverse();
                if (tcs is TaskCompletionSource<List<MessageDTO>> fmsg_tcs) fmsg_tcs.SetResult(messageList);
                break;
            case "send_message":
                if (ok != true) return;
                int? messageId = rootNode?["message_id"]?.GetValue<int>();
                int? chatId = rootNode?["chat_id"]?.GetValue<int>();
                if (messageId == null) return;
                if (tcs is TaskCompletionSource<(int, int)> msg_tcs) msg_tcs.SetResult((messageId ?? -1, chatId ?? -1));
                break;
            case "edit_is_unread":
                if (ok != true) return;
                chatId = rootNode?["chat_id"]?.GetValue<int>();
                if (chatId == null) return;
                if (tcs is TaskCompletionSource<int> editread_tcs) editread_tcs.SetResult(chatId ?? -1);
                break;
        }
        // parse events
        if (r_event != null)
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
                id = rootNode?["message_id"]?.GetValue<int>();
                if (chat == null || id == null) return;
                WeakReferenceMessenger.Default.Send(new Msg_MarkAsReadEvent { chat = chat ?? -1, id = id ?? -1 });
                break;
        }
    }
}
