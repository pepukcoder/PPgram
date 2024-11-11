using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.User;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;

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
        ObservableCollection<ChatItem> chain = [];
        foreach (MessageModel message in messages)
        {
            SetRole(message, new(messages));
            chain.Add(message);
        }
        foreach (MessageModel message in messages)
        {
            int currentIndex = messages.IndexOf(message);
            if (currentIndex > 0)
            {
                MessageModel previous = messages[currentIndex - 1];
                if (DateTimeOffset.FromUnixTimeSeconds(message.Time).Date == DateTimeOffset.FromUnixTimeSeconds(previous.Time).Date) continue;
            } 
            chain.Insert(currentIndex, new DateBadgeModel() { Date = message.Time });
        }
        return chain;
    }
    public void AddChain(ObservableCollection<ChatItem> chat)
    {
        var message = chat.OfType<MessageModel>().LastOrDefault();
        if (message == null) return;
        SetRole(message, chat);
        SetBadge(message, chat);
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
                if (previous is DateBadgeModel) message.Role = Shared.MessageRole.OwnFirst;
                if (previous is MessageModel prevm 
                    && prevm.Role != Shared.MessageRole.OwnFirst 
                    && prevm.Role != Shared.MessageRole.Own) message.Role = Shared.MessageRole.OwnFirst;
                else message.Role = Shared.MessageRole.Own;
            }
        }
        else
        {
            if (currentIndex == 0) message.Role = Shared.MessageRole.UserFirst;
            else if (currentIndex > 0)
            {
                previous = chat[currentIndex - 1];
                if (previous is DateBadgeModel) message.Role = Shared.MessageRole.UserFirst;
                if (previous is MessageModel prevm
                    && prevm.Role != Shared.MessageRole.UserFirst
                    && prevm.Role != Shared.MessageRole.User) message.Role = Shared.MessageRole.UserFirst;
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
}
