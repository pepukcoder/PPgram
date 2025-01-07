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
internal class MessageChainManager
{
    private readonly ProfileState profileState = ProfileState.Instance;
    public ObservableCollection<ChatItem> GenerateChain(List<MessageModel> messages, ChatModel? chat)
    {
        if (chat == null) return [];
        ObservableCollection<ChatItem> chain = new(messages);

        foreach (MessageModel message in messages)
        {
            SetSender(message, chat);
            SetBadge(message, chain);
        }
        foreach (ChatItem item in chain.Reverse())
        {
            if (item is DateBadgeModel) continue;
            else if (item is MessageModel message)
            {
                SetRole(message, chain, chat);
                SetReply(message, chain);
            }
        }
        return chain;
    }
    public void AddChain(MessageModel message, ObservableCollection<ChatItem> messages, ChatModel chat)
    {
        messages.Insert(0, message);
        SetSender(message, chat);
        SetBadge(message, messages);
        SetRole(message, messages, chat);
        SetReply(message, messages);
    }
    public void DeleteChain(MessageModel message, ObservableCollection<ChatItem> messages, ChatModel chat)
    {
        int currentIndex = messages.IndexOf(message);
        ChatItem prev = messages[currentIndex + 1];

        if (currentIndex > 0)
        {
            ChatItem next = messages[currentIndex - 1];
            if (next is MessageModel msg)
            {
                messages.Remove(message);
                SetRole(msg, messages, chat);
            }
            else if (prev is DateBadgeModel && next is DateBadgeModel)
            {
                messages.Remove(prev);
                messages.Remove(message);
            }
        }
        else if (prev is DateBadgeModel)
        {
            messages.Remove(prev);
            messages.Remove(message);
        }
        else
        {
            messages.Remove(message);
        }
    }
    private void SetRole(MessageModel message, ObservableCollection<ChatItem> messages, ChatModel chat)
    {
        int currentIndex = messages.IndexOf(message);
        ChatItem previous;
        if (currentIndex < messages.Count - 1)
        { 
            if (message.SenderId == profileState.UserId)
            {
                previous = messages[currentIndex + 1];
                if (previous is MessageModel prevm
                    && prevm.Role != MessageRole.OwnFirst
                    && prevm.Role != MessageRole.Own) message.Role = MessageRole.OwnFirst;
                else if (previous is DateBadgeModel) message.Role = MessageRole.OwnFirst;
                else message.Role = MessageRole.Own;
            }
            else
            {
                // TODO: assign message role for groups
                previous = messages[currentIndex + 1];
                if (previous is MessageModel prevm
                    && prevm.Role != MessageRole.UserFirst
                    && prevm.Role != MessageRole.User) message.Role = MessageRole.UserFirst;
                else if (previous is DateBadgeModel) message.Role = MessageRole.UserFirst;
                else message.Role = MessageRole.User;
                message.Status = message.Status == MessageStatus.Delivered ? MessageStatus.UnReadInvisible : MessageStatus.ReadInvisible;
            }
        }
    }
    private static void SetBadge(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        int currentIndex = chat.IndexOf(message);
        if (currentIndex < chat.Count - 1)
        {
            ChatItem previous = chat[currentIndex + 1];
            if (previous is DateBadgeModel) return;
            if (previous is MessageModel prevm
                && DateTimeOffset.FromUnixTimeSeconds(message.Time).Date == DateTimeOffset.FromUnixTimeSeconds(prevm.Time).Date) return;
        }
        chat.Insert(currentIndex + 1, new DateBadgeModel() { Date = message.Time });
    }
    private static void SetReply(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        if (message.ReplyTo == 0) return;
        MessageModel? origin = chat.OfType<MessageModel>().FirstOrDefault(m => m.Id == message.ReplyTo);
        if (origin == null) return;
        string text;
        if (origin.Content is ITextContent content) text = content.Text;
        else if (origin.Content is FileContentModel) text = "Files";
        else text = string.Empty;
        message.Reply = new()
        {
            Name = origin.Sender.Name,
            Text = text
        };
    }
    private static void SetSender(MessageModel message, ChatModel chat)
    {
        if (chat is UserModel user) message.Sender = user.Profile;
        // TODO: assign sender in group from members list
    }
}
