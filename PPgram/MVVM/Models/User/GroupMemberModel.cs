using PPgram.Shared;

namespace PPgram.MVVM.Models.User;

internal class GroupMemberModel
{
    public ProfileModel Profile { get; set; } = new();
    public ChatRole Role { get; set; }
}
