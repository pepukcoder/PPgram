using System.Collections.ObjectModel;
using PPgram.MVVM.Models.User;

namespace PPgram.MVVM.Models.Chat;

internal class GroupModel : ChatModel
{
    public bool Private { get; set; }
    public string Link { get; set; } = string.Empty;
    public ObservableCollection<GroupMemberModel> Members { get; set; } = [];
}
