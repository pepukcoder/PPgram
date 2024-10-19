using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using PPgram.Net.DTO;
using PPgram.Shared;
using System.Diagnostics;

namespace PPgram.Net;

internal class Client
{
    private string host = string.Empty;
    private int port;
    private readonly TcpClient client = new();
    private NetworkStream? stream;
    public void Connect(string remoteHost, int remotePort)
    {
        host = remoteHost;
        port = remotePort;
        try
        {
            // try to connect the socket
            if (!client.ConnectAsync(host, port).Wait(5000)) throw new Exception();
            stream = client.GetStream();
            // make a background thread for connection handling
            Thread listenThread = new(new ThreadStart(Listen))
            { IsBackground = true };
            listenThread.Start();
        }
        catch { Disconnected(); }
    }
    private void Listen()
    {
        if (stream == null) return;
        // init reponse chunks
        List<byte> response_chunks = [];
        int expected_size = 0;
        bool isFirst = true;
        while (true)
        {
            try
            {
                int read_count;
                // get server response length if chunk is first
                if (isFirst)
                {
                    byte[] length_bytes = new byte[4];
                    read_count = stream.Read(length_bytes, 0, 4);
                    if (read_count == 0) break;
                    if (BitConverter.IsLittleEndian) Array.Reverse(length_bytes);
                    int length = BitConverter.ToInt32(length_bytes);
                    // set response size
                    expected_size = length;
                    isFirst = false;
                }
                // get server response chunk
                byte[] responseBytes = new byte[expected_size];
                read_count = stream.Read(responseBytes, 0, expected_size);
                // cut chunk by actual read count and to list 
                ArraySegment<byte> segment = new(responseBytes, 0, read_count);
                responseBytes = [.. segment];
                response_chunks.AddRange(responseBytes);
                // check if whole response was recieved
                if (response_chunks.Count >= expected_size)
                {
                    // get whole response if size equals expected
                    string response = Encoding.UTF8.GetString(response_chunks.ToArray());
                    // reset size and chunks
                    response_chunks.Clear();
                    expected_size = 0;
                    isFirst = true;
                    // handle response
                    HandleResponse(response);
                }
            }
            catch { Disconnected(); }
        }
    }
    private void Send(object data)
    {
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(RequestBuilder.GetBytes(request));
        }
        catch { Disconnected(); }
    }
    private void Disconnected()
    {
        // show disconnected dialog
        WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
        {
            icon = DialogIcons.Error,
            header = "Connection error",
            text = "Unable to connect to the server",
            accept = "Retry",
            decline = ""
        });
        // listen for retry action
        WeakReferenceMessenger.Default.Register<Msg_DialogResult>(this, (r, e) =>
        {
            WeakReferenceMessenger.Default.Unregister<Msg_DialogResult>(this);
            if (e.action == DialogAction.Accepted) Connect(host, port);
        });
    }
    private void Stop()
    {
        stream?.Dispose();
        client.Close();
        Disconnected();
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
    public void ChekUsername(string username)
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
    private void HandleResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();  

        // LATER: remove debug errors dialogs and insted proccess them in mainview

        // parse specific fields
        switch (r_method)
        {
            case "login":
                if (ok == true)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_AuthResult
                    {
                        sessionId = rootNode?["session_id"]?.GetValue<string>() ?? string.Empty,
                        userId = rootNode?["user_id"]?.GetValue<int>() ?? 0
                    });
                }
                else if (ok == false && r_error != null)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Error,
                        header = "Auth error",
                        text = "Wrong username or password",
                        decline = ""
                    });
                }
                break;
            case "register":
                if (ok == true)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_AuthResult
                    {
                        sessionId = rootNode?["session_id"]?.GetValue<string>() ?? string.Empty,
                        userId = rootNode?["user_id"]?.GetValue<int>() ?? 0
                    });
                }
                else if (ok == false && r_error != null)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Error,
                        header = "Auth error",
                        text = "Registration failed",
                        decline = ""
                    });
                }
                break;
            case "auth":
                if (ok == true)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_AuthResult{ auto = true });
                }
                else if (ok == false && r_error != null)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Error,
                        header = "Auth error",
                        text = "Auto authentification failed",
                        decline = ""
                    });
                }
                break;
            case "check_username":
                if (ok == true) WeakReferenceMessenger.Default.Send(new Msg_CheckResult { available = false });
                else if (ok == false) WeakReferenceMessenger.Default.Send(new Msg_CheckResult { available = true });   
                break;
            case "fetch_self":
                if (ok == true)
                {
                    JsonNode? userNode = rootNode?["data"];
                    WeakReferenceMessenger.Default.Send(new Msg_FetchSelfResult
                    {
                        profile = new ProfileDTO
                        {
                            Name = userNode?["name"]?.GetValue<string>(),
                            Username = userNode?["username"]?.GetValue<string>(),
                            Id = userNode?["user_id"]?.GetValue<int>(),
                            Photo = userNode?["photo"]?.GetValue<string>()
                        }
                    });
                    Debug.WriteLine(response);
                }
                else if (ok == false && r_error != null)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Error,
                        header = "Fetch error",
                        text = "Fetch self failed",
                        decline = ""
                    });
                }
                break;
            case "fetch_chats":
                if (ok == true)
                {
                    JsonArray? chatsJson = rootNode?["data"]?.AsArray();
                    List<ChatDTO> chatlist = [];
                    if (chatsJson == null) return;
                    foreach (JsonNode? chatNode in chatsJson)
                    {
                        ChatDTO? chat = chatNode?.Deserialize<ChatDTO>();
                        if (chat != null) chatlist.Add(chat);
                    }
                    WeakReferenceMessenger.Default.Send(new Msg_FetchChatsResult { chats = chatlist });
                }
                else if (ok == false && r_error != null)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Error,
                        header = "Fetch error",
                        text = "Fetch chats failed",
                        decline = ""
                    });
                }
                break;
            /*case "fetch_messages":
                if (ok == true)
                {
                    JsonArray? messagesJson = rootNode?["data"]?.AsArray();
                    ObservableCollection<MessageDTO> messagelist = [];
                    if (messagesJson != null)
                    {
                        foreach (JsonNode? chatNode in messagesJson)
                        {
                            MessageDTO? message = chatNode?.Deserialize<MessageDTO>();
                            if (message != null)
                                messagelist.Add(message);
                        }
                    }
                    MessagesFetched?.Invoke(this, new ResponseFetchMessagesEventArgs
                    {
                        ok = true,
                        messages = messagelist
                    });
                }
                else if (ok == false && r_error != null)
                {
                    MessagesFetched?.Invoke(this, new ResponseFetchMessagesEventArgs
                    {
                        ok = false,
                        error = r_error
                    });
                }
                break;
            case "fetch_user":
                if (ok == true)
                {
                    JsonNode? userNode = rootNode?["data"];
                    GotNewChat?.Invoke(this, new GotChatEventArgs
                    {
                        ok = true,
                        chat = new ChatDTO
                        {
                            Name = userNode?["name"]?.GetValue<string>(),
                            Username = userNode?["username"]?.GetValue<string>(),
                            Id = userNode?["user_id"]?.GetValue<int>(),
                            Photo = userNode?["photo"]?.GetValue<string>()
                        }
                    });
                }
                else if (ok == false && r_error != null)
                {
                    GotNewChat?.Invoke(this, new GotChatEventArgs
                    {
                        ok = false,
                        error = r_error
                    });
                }
                break;
        }
        switch (r_event)
        {
            case "new_message":
                if (ok == true)
                {
                    JsonNode? messageJson = rootNode?["data"];
                    if (messageJson != null)
                    {
                        MessageDTO? message_dto = messageJson.Deserialize<MessageDTO>();
                        NewMessage?.Invoke(this, new NewMessageEventArgs
                        {
                            ok = true,
                            message = message_dto
                        });
                    }
                }
                else if (ok == false && r_error != null)
                {
                    NewMessage?.Invoke(this, new NewMessageEventArgs
                    {
                        ok = false,
                        error = r_error
                    });
                }
                break;
            case "new_chat":
                if (ok == true)
                {
                    JsonNode? chatNode = rootNode?["data"];
                    GotNewChat?.Invoke(this, new GotChatEventArgs
                    {
                        ok = true,
                        chat = chatNode?.Deserialize<ChatDTO>()
                    });
                }
                else if (ok == false && r_error != null)
                {
                    GotNewChat?.Invoke(this, new GotChatEventArgs
                    {
                        ok = false,
                        error = r_error
                    });
                }
                break; */
        }
    }
}