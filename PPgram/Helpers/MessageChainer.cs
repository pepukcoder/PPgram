using Avalonia.Remote.Protocol.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace PPgram.Helpers;

/// <summary>
/// Provides general functions to create and update message chains, badges etc.
/// </summary>
internal static class MessageChainer
{
    private static readonly ProfileState profileState = ProfileState.Instance;
    public static void AddChain(List<MessageModel> newMessages, ObservableCollection<ChatItem> messages, ChatModel chat, bool forward)
    {
        // get anchor message from one of the edges
        int anchor;
        if (forward) anchor = 0;
        else anchor = messages.Count > 0 ? messages.Count - 1 : 0;
        // add and chain new messages
        for (int index = newMessages.Count - 1; index >= 0; index--)
        {
            MessageModel current = newMessages[index];
            messages.Insert(anchor, current);
            SetSender(current, chat);
            SetBadge(current, messages);
            SetRole(current, messages, chat);
            SetReply(current, messages);
        }
        // fix anchor message after inserting new ones 
        if (!forward && anchor > 0)
        {
            if (messages[anchor - 1] is DateBadgeModel badge)
            {
                messages.Remove(badge);
            }
            if (messages[anchor - 1] is MessageModel msg)
            {
                SetBadge(msg, messages);
                SetRole(msg, messages, chat);
            }
        }
    }
    public static void DeleteChain(MessageModel message, ObservableCollection<ChatItem> messages, ChatModel chat)
    {
        int index = messages.IndexOf(message);
        if (index < messages.Count - 1 && messages[index + 1] is DateBadgeModel prev)
        {
            messages.Remove(prev);
        }
        if (index > 0 && messages[index - 1] is MessageModel msg)
        {
            SetRole(msg, messages, chat);
        }
        messages.Remove(message);
    }
    private static void SetSender(MessageModel message, ChatModel chat)
    {
        if (chat is UserModel user) message.Sender = user.Profile;
        // TODO: assign sender in group from members list
    }
    private static void SetBadge(MessageModel message, ObservableCollection<ChatItem> messages)
    {
        int index = messages.IndexOf(message);
        if (index < messages.Count - 1)
        {
            ChatItem previous = messages[index + 1];
            if (previous is DateBadgeModel) return;
            if (previous is MessageModel msg)
            {
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(message.Time).Date;
                DateTime prevTime = DateTimeOffset.FromUnixTimeSeconds(msg.Time).Date;
                if (time.Day == prevTime.Day) return;
            }
        }
        messages.Insert(index + 1, new DateBadgeModel() { Date = message.Time });
    }
    private static void SetRole(MessageModel message, ObservableCollection<ChatItem> messages, ChatModel chat)
    {
        MessageRole[] roles = [MessageRole.UserFirst, MessageRole.User];
        int currentIndex = messages.IndexOf(message);
        if (currentIndex < messages.Count - 1)
        {
            ChatItem previous = messages[currentIndex + 1];
            if (chat is GroupModel group) roles = [MessageRole.GroupFirst, MessageRole.Group];
            if (message.SenderId == profileState.UserId) roles = [MessageRole.OwnFirst, MessageRole.Own];
            else message.Status = message.Status == MessageStatus.Read ? MessageStatus.ReadInvisible : MessageStatus.UnReadInvisible;
            if (previous is MessageModel prevm && roles.All(r => r != prevm.Role)) message.Role = roles[0];
            else if (previous is DateBadgeModel) message.Role = roles[0];
            else message.Role = roles[1];
        }
    }
    private static void SetReply(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        if (message.ReplyTo == -1) return;
        MessageModel? origin = chat.OfType<MessageModel>().FirstOrDefault(m => m.Id == message.ReplyTo);
        if (origin == null) return;
        string text;
        if (origin.Content is ITextContent content) text = content.Text;
        else if (origin.Content is FileContentModel fc) text = $"{fc.Files.Count} File(s)";
        else text = string.Empty;
        message.Reply = new()
        {
            Name = origin.Sender.Name,
            Text = text
        };
    }
}
