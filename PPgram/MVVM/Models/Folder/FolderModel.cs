using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.Chat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.Folder;
/// <summary>
/// Represents a chat folder
/// </summary>
/// <param name="name">Folder name</param>
internal partial class FolderModel : ObservableObject
{
    [ObservableProperty]
    private string name;
    [ObservableProperty]
    ObservableCollection<ChatModel> chats = [];

    public bool IsSpecial { get; set; }
    public bool IsAll { get; set; }
    public bool IsPersonal { get; set; }
    public string Id { get; set; }
    private readonly HashSet<int> chat_ids = [];

    public FolderModel(string name, bool is_all = false, bool is_personal = false)
    {
        Name = name;
        IsSpecial = is_all || is_personal;
        IsAll = is_all;
        IsPersonal = is_personal;
        Id = Guid.NewGuid().ToString();
    }
    public FolderModel(FolderData data)
    {
        Name = data.Name;
        Id = data.Id;
        chat_ids = data.Chats;
    }
    public bool TryAssign(ChatModel chat)
    {
        if (IsAll || (IsPersonal && chat is UserModel) || chat_ids.Contains(chat.Id))
        {
            Chats.Add(chat);
            return true;
        }
        return false;
    }
    public void Add(ChatModel chat)
    {
        chat_ids.Add(chat.Id);
    }
    public void Remove(ChatModel chat)
    {
        chat_ids.Remove(chat.Id);
    }
}
