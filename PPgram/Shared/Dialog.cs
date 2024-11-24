using Avalonia.Layout;
using PPgram.MVVM.Models.Dialog;

namespace PPgram.Shared;

class Msg_OpenAttachFiles;
class Msg_SendAttachFiles
{
    public required string description;
}
class Msg_CloseDialog;
class Msg_ShowDialog
{
    public required Dialog dialog;
}
