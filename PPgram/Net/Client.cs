using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using PPgram.Shared;

namespace PPgram.Net;

internal class Client
{
    private readonly TcpClient client = new();
    private NetworkStream? stream;
    public void Connect(string host, int port)
    {
        try
        {
            // try to connect the socket
            client.Connect(host, port);
            stream = client.GetStream();
            // make a background thread for connection handling
            Thread listenThread = new(new ThreadStart(Listen))
            { IsBackground = true };
            listenThread.Start();
        }
        catch
        {
            // show dialog on error
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                icon = DialogIcons.Error,
                header = "Connection error",
                text = "Unable to connect to the server",
                accept = "Retry",
                decline = ""
            });
            // listen for retry dialog action
            WeakReferenceMessenger.Default.Register<Msg_DialogResult>(this, (r, e) =>
            {
                WeakReferenceMessenger.Default.Unregister<Msg_DialogResult>(this);
                if (e.action == DialogAction.Accepted)
                {
                    Connect(host, port);
                }
            });
        }
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

                if (response_chunks.Count >= expected_size)
                {
                    // get whole response if size equals expected
                    string response = Encoding.UTF8.GetString(response_chunks.ToArray());

                    // reset chunks
                    response_chunks.Clear();
                    expected_size = 0;
                    isFirst = true;

                    // handle response
                    HandleResponse(response);
                }
            }
            catch
            {
                // Disconnected?.Invoke(this, new());
            }
        }
    }
    private void Send(object data)
    {
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(RequestBuilder.GetBytes(request));
        }
        catch
        {
            // Disconnected?.Invoke(this, new());
        }

    }
    private void Stop()
    {
        // Disconnected?.Invoke(this, new EventArgs());
        stream?.Dispose();
        client.Close();
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
    public void AuthLogin(string login, string password)
    {
        var data = new
        {
            method = "login",
            username = login,
            password_hash = password
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
            password_hash = new_password
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
    private void HandleResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();
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
                if (ok == true)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_CheckResult
                    {
                        available = false
                    });
                }
                else if (ok == false)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_CheckResult
                    {
                        available = true
                    });
                }
                break;
            /*case "fetch_self":
                if (ok == true)
                {
                    JsonNode? userNode = rootNode?["data"];
                    SelfFetched?.Invoke(this, new ResponseFetchUserEventArgs
                    {
                        ok = true,
                        user = new ProfileDTO
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
                    SelfFetched?.Invoke(this, new ResponseFetchUserEventArgs
                    {
                        ok = false,
                        error = r_error
                    });
                }
                break;
            case "fetch_chats":
                if (ok == true)
                {
                    // SHITCODE CLEANING NEEDED
                    JsonArray? chatsJson = rootNode?["data"]?.AsArray();
                    ObservableCollection<ChatDTO> chatlist = [];
                    if (chatsJson != null)
                    {
                        foreach (JsonNode? chatNode in chatsJson)
                        {
                            ChatDTO? chat = chatNode?.Deserialize<ChatDTO>();
                            if (chat != null)
                            {
                                chatlist.Add(chat);
                            }
                        }
                        ChatsFetched?.Invoke(this, new ResponseFetchChatsEventArgs
                        {
                            ok = true,
                            chats = chatlist
                        });
                    }
                }
                else if (ok == false && r_error != null)
                {
                    ChatsFetched?.Invoke(this, new ResponseFetchChatsEventArgs
                    {
                        ok = true,
                        error = r_error
                    });
                }
                break;
            case "fetch_messages":
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