using PPgram.App;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace PPgram.Helpers;

/// <summary>
/// Provides general functions to create and update message chains, badges etc.
/// </summary>
internal class MessageChainManager
{
    private readonly ProfileState profileState = ProfileState.Instance;
    public ObservableCollection<ChatItem> GenerateChain(ObservableCollection<MessageModel> messages, ChatModel? chat)
    {
        if (chat == null) return [];
        ObservableCollection<ChatItem> chain = new(messages);

        foreach (MessageModel message in messages)
        {
            SetBadge(message, chain);
        }
        foreach (ChatItem item in chain)
        {
            if (item is DateBadgeModel) continue;
            else if (item is MessageModel message)
            {
                SetRole(message, chain);
                SetReply(message, chain);
            }
        }
        return chain;
    }
    public void AddChain(ObservableCollection<ChatItem> chat)
    {
        MessageModel? message = chat.OfType<MessageModel>().LastOrDefault();
        if (message == null) return;
        SetBadge(message, chat);
        SetRole(message, chat);
        SetReply(message, chat);
    }
    public void DeleteChain(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        // possible improvements here

        int currentIndex = chat.IndexOf(message);
        ChatItem prev = chat[currentIndex - 1];

        if (currentIndex != chat.Count - 1)
        {
            ChatItem next = chat[currentIndex + 1];
            if (next is MessageModel msg)
            {
                Debug.WriteLine(msg.Id);
                chat.Remove(message);
                SetRole(msg, chat);
            }
            else if (prev is DateBadgeModel && next is DateBadgeModel)
            {
                chat.Remove(prev);
                chat.Remove(message);
            }
        }
        else if (prev is DateBadgeModel)
        {
            chat.Remove(prev);
            chat.Remove(message);
        }
        else
        {
            chat.Remove(message);
        }
    }
    private void SetRole(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        int currentIndex = chat.IndexOf(message);
        ChatItem previous;
        if (message.SenderId == profileState.UserId)
        {
            if (currentIndex == 0) message.Role = Shared.MessageRole.OwnFirst;
            else if (currentIndex > 0)
            {
                previous = chat[currentIndex - 1];
                if (previous is MessageModel prevm 
                    && prevm.Role != Shared.MessageRole.OwnFirst 
                    && prevm.Role != Shared.MessageRole.Own) message.Role = Shared.MessageRole.OwnFirst;
                else if (previous is DateBadgeModel) message.Role = Shared.MessageRole.OwnFirst;
                else message.Role = Shared.MessageRole.Own;
            }
        }
        else
        {
            if (currentIndex == 0) message.Role = Shared.MessageRole.UserFirst;
            else if (currentIndex > 0)
            {
                previous = chat[currentIndex - 1];
                if (previous is MessageModel prevm
                    && prevm.Role != Shared.MessageRole.UserFirst
                    && prevm.Role != Shared.MessageRole.User) message.Role = Shared.MessageRole.UserFirst;
                else if (previous is DateBadgeModel) message.Role = Shared.MessageRole.UserFirst;     
                else message.Role = Shared.MessageRole.User;     
            }
            message.Status = Shared.MessageStatus.None;
        }
    }
    private void SetBadge(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        int currentIndex = chat.IndexOf(message);
        if (currentIndex > 0)
        {
            ChatItem previous = chat[currentIndex - 1];
            if (previous is DateBadgeModel) return;
            if (previous is MessageModel prevm 
                && DateTimeOffset.FromUnixTimeSeconds(message.Time).Date == DateTimeOffset.FromUnixTimeSeconds(prevm.Time).Date) return;
        }
        chat.Insert(currentIndex, new DateBadgeModel() { Date = message.Time });
    }
    private void SetReply(MessageModel message, ObservableCollection<ChatItem> chat)
    {
        if (message.ReplyTo == 0) return;
        MessageModel? replied = chat.OfType<MessageModel>().FirstOrDefault(m => m.Id == message.ReplyTo);
        if (replied == null) return;

        string text;
        if (replied.Content is ITextContent content) text = content.Text;
        else if (replied.Content is FileContentModel) text = "Files";
        else text = string.Empty;

        message.Reply = new()
        {
            Name = replied.Sender.Name,
            Text = text
        };
    }
}
