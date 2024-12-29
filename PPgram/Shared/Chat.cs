namespace PPgram.Shared;

class Msg_SendDraft
{
    public required string draft;
    public required int chat_id;
}
public enum ChatType
{
    Chat,
    Group,
    Channel
}
public enum ChatStatus
{
    None,
    Typing,
}
public enum GroupRole
{
    None,
    Admin,
    Owner
}