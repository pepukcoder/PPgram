using CommunityToolkit.Mvvm.Messaging;
using PPgram.Net.DTO;
using PPgram.Shared;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PPgram.Net;

internal class JsonClient : BaseClient
{
    private void Send(object data)
    {
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(BuildJsonRequest(request));
        }
        catch { Disconnected(); }
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
    protected override void HandleResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();  

        // LATER: remove debug errors dialogs and insted proccess them in mainview

        if (ok == false && r_method != null && r_error != null) {
            string method = r_method;
            string error = r_error;

            string result;
            #if DEBUG
                result = $"[DEBUG] Error in method: {method}\n Error:{error}";
            #else
                result = $"Error: {error}";
            #endif
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                icon = DialogIcons.Error,
                header = "Error occured!",
                text = result,
                decline = ""
            });
            return;
        }

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
                    WeakReferenceMessenger.Default.Send(new Msg_FetchSelfResult
                    {  
                        profile = rootNode?.Deserialize<ProfileDTO>()
                    });
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
            case "fetch_users":
                if (ok == true)
                {
                    JsonArray? usersJson = rootNode?["users"]?.AsArray();
                    List<ProfileDTO> userlist = [];
                    if (usersJson == null) return;
                    foreach (JsonNode? userNode in usersJson)
                    {
                        ProfileDTO? user = userNode?.Deserialize<ProfileDTO>();
                        if (user != null) userlist.Add(user);
                    }
                    WeakReferenceMessenger.Default.Send(new Msg_SearchChatsResult{ users = userlist });
                }
                else if (ok == false && r_error != null)
                {
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Error,
                        header = "Fetch error",
                        text = "Search failed",
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