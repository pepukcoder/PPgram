using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.Message;

namespace PPgram.Shared;

class Msg_ShowDialog
{
    public required Dialog dialog;
    public int time = 0;
}
class Msg_CloseDialog;
class Msg_OpenAttachFiles;
class Msg_OpenForward
{
    public required MessageModel message;
}
public enum DialogIcons
{
    None,
    Error,
    Check,
    Info,
    Notify
}