namespace PPgram.Net;

internal class PPRequest
{
    public required RequestType Type { get; set; }
    public required object CompletitionSource { get; set; }
}
internal enum RequestType
{
    Undefined,
    Auth,
    AuthSession,
    CheckUsername,
    FetchSelf,
    FetchChats,
    FetchMessages,
    SendMessage,
    EditMessage,
    DeleteMessage,
    EditSelf,
    ReadMessage,
    SendDraft,
}