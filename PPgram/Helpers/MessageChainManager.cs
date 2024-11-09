using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.User;
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
        ObservableCollection<ChatItem> chain = [];
        foreach (MessageModel message in messages)
        {
            if (message.SenderId == profileState.UserId)
            {
                if (messages.IndexOf(message) == 0 ) message.Role = Shared.MessageRole.OwnFirst;
                else message.Role = Shared.MessageRole.Own;
            }
            else
            {
                if (messages.IndexOf(message) == 0 ) message.Role = Shared.MessageRole.UserFirst;
                else message.Role = Shared.MessageRole.User;

                message.Status = Shared.MessageStatus.None;
            }
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
}
