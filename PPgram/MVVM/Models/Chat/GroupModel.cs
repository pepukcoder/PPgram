using System.Collections.ObjectModel;
using PPgram.MVVM.Models.User;

namespace PPgram.MVVM.Models.Chat;

internal class GroupModel : ChatModel
{
    public bool Private { get; set; }
    public string Link { get; set; } = string.Empty;
    private string _lastSender = string.Empty;
    public string LastSender
    {
        get => _lastSender + ":";
        set => _lastSender = value;
    }
    public string LastMessage { get; set; } = string.Empty;
    public ObservableCollection<GroupMemberModel> Members { get; set; } = [];
}
