﻿using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.Dialog;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace PPgram.Net;

internal class JsonClient
{
    private string host = string.Empty;
    private int port;
    private TcpClient? client;
    private NetworkStream? stream;
    public void Connect(string remoteHost, int remotePort)
    {
        host = remoteHost;
        port = remotePort;
        try
        {
            if (client != null) return;
            client = new();
            if (!client.ConnectAsync(host, port).Wait(5000)) throw new TimeoutException("Connection to server timed out");
            stream = client.GetStream();
            Thread listenThread = new(new ThreadStart(Listen)) { IsBackground = true };
            listenThread.Start();
        }
        catch { Disconnect(); }
    }
    private void Listen()
    {
        JsonConnection connection = new();
        while (client != null)
        {
            try
            {
                connection.ReadStream(stream);
                if (connection.IsReady) HandleResponse(connection.GetResponseAsString());
            }
            catch { Disconnect(); }
        }
    }
    public void Disconnect()
    {
        WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
        {
            dialog = new ConnectionDialog
            {
                Position = Avalonia.Layout.VerticalAlignment.Bottom,
                canSkip = false
            }
        });
        if (client?.Client.Connected == true) client?.Client.Disconnect(false);
        client = null;
    }
    private void Send(object data)
    {
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(JsonConnection.BuildJsonRequest(request));
        }
        catch { Disconnect(); }
    }
    public void AuthSessionId(string sessionId, int userId)
    {
        var data = new
        {
            method = "auth",
            user_id = userId,
            session_id = sessionId,
        };
        Send(data);
    }
    public void AuthLogin(string login_username, string login_password)
    {
        var data = new
        {
            method = "login",
            username = login_username,
            password = login_password
        };
        Send(data);
    }
    public void RegisterUser(string new_username, string new_name, string new_password)
    {
        var data = new
        {
            method = "register",
            username= new_username,
            name = new_name,
            password = new_password
        };
        Send(data);
    }
    public void CheckUsername(string username)
    {
        var data = new
        {
            method = "check",
            what = "username",
            data = username
        };
        Send(data);
    }
    public void FetchSelf()
    {
        var data = new
        {
            method = "fetch",
            what = "self"
        };
        Send(data);
    }
    public void FetchChats()
    {
        var data = new
        {
            method = "fetch",
            what = "chats"
        };
        Send(data);
    }
    public void SearchChats(string searchQuery)
    {
        var data = new
        {
            method = "fetch",
            what = "users",
            query = searchQuery
        };
        Send(data);
    }
    public void FetchMessages(int id, int[] fetchRange)
    {
        var data = new
        {
            method = "fetch",
            what = "messages",
            chat_id = id,
            range = fetchRange
        };
        Send(data);
    }
    public void SendMessage(int chatId, int? replyTo, string text, List<string> hashes)
    {
        var data = new
        {
            method = "send_message",
            to = chatId,
            reply_to = replyTo != null ? replyTo : null,
            content = new
            {
                text = text,
                sha256_hashes = hashes.ToArray(),
            }
        };
        Send(data);
    }
    public void EditMessage(int chatId, int messageId, string new_text)
    {
        var data = new
        {
            method = "edit",
            what = "message",
            chat_id = chatId,
            message_id = messageId,
            content = new_text
        };
        Send(data);
    }
    public void DeleteMessage(int chatId, int messageId)
    {
        var data = new
        {
            method = "delete",
            chat_id = chatId,
            message_id = messageId,
        };
        Send(data);
    }
    private void HandleResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();

        // LATER: remove debug errors dialogs and instead proccess them in mainviewmodel

        if (ok == false && r_method != null && r_error != null) {
            string method = r_method;
            string error = r_error;

            string result;
            #if DEBUG
                result = $"[DEBUG] Error in method: {method}\n Error:{error}";
            #else
                result = $"Error: {error}";
            #endif
            /* DIALOGFIX
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                icon = DialogIcons.Error,
                header = "Error occured!",
                text = result,
                decline = ""
            });
            */
            return;
        }

        Debug.WriteLine(response);

        // parse specific fields
        switch (r_method)
        {
            case "login":
                if (ok != true) return;
                WeakReferenceMessenger.Default.Send(new Msg_AuthResult
                {
                    sessionId = rootNode?["session_id"]?.GetValue<string>() ?? string.Empty,
                    userId = rootNode?["user_id"]?.GetValue<int>() ?? 0
                });
                break;
            case "register":
                if (ok != true) return;
                WeakReferenceMessenger.Default.Send(new Msg_AuthResult
                {
                    sessionId = rootNode?["session_id"]?.GetValue<string>() ?? string.Empty,
                    userId = rootNode?["user_id"]?.GetValue<int>() ?? 0
                });
                break;
            case "auth":
                if (ok != true) return;
                WeakReferenceMessenger.Default.Send(new Msg_AuthResult { auto = true });
                break;
            case "check_username":
                if (ok == true) WeakReferenceMessenger.Default.Send(new Msg_CheckResult { available = false });
                else if (ok == false) WeakReferenceMessenger.Default.Send(new Msg_CheckResult { available = true });
                break;
            case "fetch_self":
                if (ok != true) return;
                WeakReferenceMessenger.Default.Send(new Msg_FetchSelfResult
                {
                    profile = rootNode?.Deserialize<ProfileDTO>()
                });
                break;
            case "fetch_chats":
                if (ok != true) return;
                JsonArray? chatsJson = rootNode?["chats"]?.AsArray();
                List<ChatDTO> chatlist = [];
                if (chatsJson == null) return;
                foreach (JsonNode? chatNode in chatsJson)
                {
                    ChatDTO? chat = chatNode?.Deserialize<ChatDTO>();
                    if (chat != null) chatlist.Add(chat);
                }
                WeakReferenceMessenger.Default.Send(new Msg_FetchChatsResult { chats = chatlist });
                break;
            case "fetch_users":
                if (ok != true) return;
                JsonArray? usersJson = rootNode?["users"]?.AsArray();
                List<ChatDTO> userList = [];
                if (usersJson != null && usersJson.Count != 0)
                {
                    foreach (JsonNode? userNode in usersJson)
                    {
                        ChatDTO? user = userNode?.Deserialize<ChatDTO>();
                        if (user != null)
                        {
                            user.Id = userNode?["user_id"]?.GetValue<int>() ?? 0;
                            userList.Add(user);
                        }
                    }
                };
                WeakReferenceMessenger.Default.Send(new Msg_SearchChatsResult { users = userList });
                break;
            case "fetch_messages":
                if (ok != true) return;
                JsonArray? messagesJson = rootNode?["messages"]?.AsArray();
                List<MessageDTO> messageList = [];
                if (messagesJson == null) return;
                foreach(JsonNode? messageNode in messagesJson)
                {
                    MessageDTO? message = messageNode?.Deserialize<MessageDTO>();
                    if (message != null) messageList.Add(message);
                }
                messageList.Reverse();
                WeakReferenceMessenger.Default.Send(new Msg_FetchMessagesResult { messages = messageList });
                break;
            case "send_message":
                if (ok != true) return;
                int? messageId = rootNode?["message_id"]?.GetValue<int>();
                int? chatId = rootNode?["chat_id"]?.GetValue<int>();
                if (messageId == null || chatId == null) return;
                WeakReferenceMessenger.Default.Send(new Msg_ChangeMessageStatus { chat = chatId ?? 0, Id = messageId ?? 0, status = MessageStatus.Delivered});
                break;
        }
        // parse events
        switch (r_event)
        {
            case "new_message":
                JsonNode? messageNode = rootNode?["new_message"];
                if (messageNode == null) return;
                MessageDTO? messageDTO = messageNode.Deserialize<MessageDTO>();
                WeakReferenceMessenger.Default.Send(new Msg_NewMessage { message = messageDTO });
                break;
            case "new_chat":
                JsonNode? chatNode = rootNode?["new_chat"];
                if (chatNode == null) return;
                ChatDTO? chatDTO = chatNode.Deserialize<ChatDTO>();
                WeakReferenceMessenger.Default.Send(new Msg_NewChat { chat = chatDTO });
                break;
            case "edit_message":
                messageNode = messageNode = rootNode?["new_message"];
                if (messageNode == null) return;
                messageDTO = messageNode.Deserialize<MessageDTO>();
                WeakReferenceMessenger.Default.Send(new Msg_EditMessageEvent { message = messageDTO });
                break;
            case "delete_message":
                int? chat = rootNode?["chat_id"]?.GetValue<int>();
                int? id = rootNode?["message_id"]?.GetValue<int>();
                if (chat == null || id == null) return;
                WeakReferenceMessenger.Default.Send(new Msg_DeleteMessageEvent { chat = chat ?? 0, Id = id ?? 0 });
                break;
        }
    }
}
