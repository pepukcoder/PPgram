using PPgram.MVVM.Models.Dialog;

namespace PPgram.Shared;

class Msg_ShowDialog
{
    public required Dialog dialog;
    public int time = 0;
}
class Msg_CloseDialog;
class Msg_Reconnect;
class Msg_OpenAttachFiles;
public enum DialogIcons
{
    None,
    Error,
    Check,
    Info,
    Notify
}