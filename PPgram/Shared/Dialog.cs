using PPgram.MVVM.Models.Dialog;

namespace PPgram.Shared;

class Msg_ShowDialog
{
    public required Dialog dialog;
}
class Msg_CloseDialog;
class Msg_Reconnect;
class Msg_OpenAttachFiles;
class Msg_RegularDialogResult
{
    public required string action;
}
