using PPgram.App;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PPgram.Helpers;

/// <summary>
/// Provides general functions to create and update message chains, badges etc.
/// </summary>
internal static class MessageChainer
{
    private static readonly ProfileState profileState = ProfileState.Instance;
    private static readonly AppState appState = AppState.Instance;
    public static void AddChain(List<MessageModel> newMessages, ObservableCollection<ChatItem> items, ChatModel chat, bool forward)
    {
        // get anchor message from one of the edges
        int anchor;
        if (forward) anchor = 0;
        else anchor = items.Count > 0 ? items.Count - 1 : 0;
        // add and chain new messages
        for (int index = newMessages.Count - 1; index >= 0; index--)
        {
            MessageModel current = newMessages[index];
            items.Insert(anchor, current);
            SetSender(current, chat);
            SetBadge(current, items);
            SetRole(current, items, chat);
            SetReply(current, items);
        }
        // fix anchor message after inserting new ones 
        if (!forward && anchor > 0)
        {
            if (items[anchor - 1] is DateBadgeModel badge)
            {
                items.Remove(badge);
            }
            if (items[anchor - 1] is MessageModel msg)
            {
                SetBadge(msg, items);
                SetRole(msg, items, chat);
            }
        }
    }
    public static void DeleteChain(MessageModel message, ObservableCollection<ChatItem> items, ChatModel chat)
    {
        int index = items.IndexOf(message);
        if (index < items.Count - 1 && items[index + 1] is DateBadgeModel prev)
        {
            items.Remove(prev);
        }
        items.Remove(message);
        if (index > 0 && items[index - 1] is MessageModel msg)
        {
            SetBadge(msg, items);
            SetRole(msg, items, chat);
        }
    }
    public static void UnloadChain(ObservableCollection<ChatItem> items, ChatModel chat)
    {
        List<MessageModel> messages = items.OfType<MessageModel>().ToList();
        for (int i = appState.MessagesFetchMinimum; i < messages.Count; i++)
        {
            DeleteChain(messages[i], items, chat);
        }
    }
    private static void SetSender(MessageModel message, ChatModel chat)
    {
        if (message.SenderId == profileState.UserId && profileState.Profile != null) message.Sender = profileState.Profile;
        else if (chat is UserModel user) message.Sender = user.Profile;
        // TODO: assign sender in group from members list
    }
    private static void SetBadge(MessageModel message, ObservableCollection<ChatItem> items)
    {
        int index = items.IndexOf(message);
        if (index < items.Count - 1)
        {
            ChatItem previous = items[index + 1];
            if (previous is DateBadgeModel) return;
            if (previous is MessageModel msg)
            {
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(message.Time).Date;
                DateTime prevTime = DateTimeOffset.FromUnixTimeSeconds(msg.Time).Date;
                if (time.Day == prevTime.Day) return;
            }
        }
        items.Insert(index + 1, new DateBadgeModel() { Date = message.Time });
    }
    private static void SetRole(MessageModel message, ObservableCollection<ChatItem> items, ChatModel chat)
    {
        MessageRole[] roles = [MessageRole.UserFirst, MessageRole.User];
        int currentIndex = items.IndexOf(message);
        if (currentIndex < items.Count - 1)
        {
            ChatItem previous = items[currentIndex + 1];
            if (chat is GroupModel group) roles = [MessageRole.GroupFirst, MessageRole.Group];
            if (message.SenderId == profileState.UserId) roles = [MessageRole.OwnFirst, MessageRole.Own];
            else message.Status = message.Status == MessageStatus.Read ? MessageStatus.ReadInvisible : MessageStatus.UnReadInvisible;
            if (previous is MessageModel prevm && roles.All(r => r != prevm.Role)) message.Role = roles[0];
            else if (previous is DateBadgeModel) message.Role = roles[0];
            else message.Role = roles[1];
        }
    }
    private static void SetReply(MessageModel message, ObservableCollection<ChatItem> items)
    {
        if (message.ReplyTo == -1) return;
        MessageModel? origin = items.OfType<MessageModel>().FirstOrDefault(m => m.Id == message.ReplyTo);
        if (origin == null) return;
        string text;
        if (origin.Content is ITextContent content) text = content.Text;
        else if (origin.Content is FileContentModel fc) text = $"{fc.Files.Count} File(s)";
        else text = string.Empty;
        message.Reply = new()
        {
            Sender = origin.Sender,
            Text = text
        };
    }
}
