﻿using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;
using System.Linq;

namespace PPgram.MVVM.Models.Chat;

/// <summary>
/// Repsenets properties of a user chat
/// </summary>
internal class UserModel : ChatModel
{
    public bool Online { get; set; }
    public string LastOnline { get; set; } = string.Empty;

    protected override void UpdateLastMessage()
    {
        MessageModel? lastmsg = Messages.OfType<MessageModel>().LastOrDefault();
        if (lastmsg == null)
        {
            LastMessage = "";
            LastMessageStatus = MessageStatus.None;
            return;
        }
        if (lastmsg.Content is ITextContent tc) LastMessage = tc.Text;
        if (lastmsg.SenderId == profileState.UserId) LastMessageStatus = lastmsg.Status;
        else LastMessageStatus = MessageStatus.None;
        LastMessageTime = lastmsg.Time;
    }
}
