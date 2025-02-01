using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.User;

namespace PPgram.Shared;

class Msg_EditSelf
{
    public required ProfileModel profile;
    public required bool avatarChanged;
}