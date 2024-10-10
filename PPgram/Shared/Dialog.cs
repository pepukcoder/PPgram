namespace PPgram.Shared;

class Msg_ShowDialog
{
    public string text = string.Empty;
    public string header = string.Empty;
    public DialogIcons icon = DialogIcons.None;
    public string accept = "Ok";
    public string decline = "Cancel";
}
class Msg_DialogResult
{
    public DialogAction action = DialogAction.Declined;
}
public enum DialogAction
{
    Declined,
    Accepted,
    TapClosed
}
public enum DialogIcons
{
    None,
    Error,
    Check,
    Info
}