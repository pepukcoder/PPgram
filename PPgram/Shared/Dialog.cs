namespace PPgram.Shared;

class Msg_ShowDialog
{
    public required string text;
    public string header = string.Empty;
    public DialogIcons icon;
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