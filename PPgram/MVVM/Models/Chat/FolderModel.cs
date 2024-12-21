using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.Chat;

internal partial class FolderModel(string name, bool is_all = false, bool is_archive = false, bool is_personal = false) : ObservableObject
{
    [ObservableProperty]
    private string name = name;
    [ObservableProperty]
    ObservableCollection<ChatModel> chats = [];

    public bool IsSpecial { get; set; } = is_all || is_archive || is_personal;
    public bool IsAll { get; set; } = is_all;
    public bool IsArchive { get; set; } = is_archive;
    public bool IsPersonal { get; set; } = is_personal;
}
