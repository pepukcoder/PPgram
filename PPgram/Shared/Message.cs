namespace PPgram.Shared;

public enum MessageType
{
    User,
    UserFirst,
    Own,
    OwnFirst,
    Group,
    GroupSingle,
    GroupFirst,
    GroupLast,
    Date
}
public enum MessageStatus
{
    None,
    Sending,
    Delivered,
    Read,
    Error
}
public enum MediaType
{
    None,
    Images,
    Files,
}