﻿using PPgram.Net.DTO;

namespace PPgram.Shared;

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
class Msg_NewChat
{
    public required ChatDTO? chat;
}