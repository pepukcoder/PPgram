using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Net.DTO;
using PPgram.Shared;
using System.Collections.Generic;

namespace PPgram.Helpers;

internal class DTOToModelConverter
{
    public static MessageModel ConvertMessage(MessageDTO messageDTO)
    { 
        MessageContentModel content;
        if (messageDTO.MediaHashes != null && messageDTO.MediaHashes.Length != 0)
        {
            List<FileModel> files = [];
            foreach (string hash in messageDTO.MediaHashes)
            {
                files.Add(new() { Hash = hash });
            }
            content = new FileContentModel()
            {
                Files = new(files),
                Text = messageDTO.Text ?? string.Empty
            };
        }
        else
        {
            content = new TextContentModel()
            {
                Text = messageDTO.Text ?? string.Empty
            };
        }
        return new MessageModel()
        {
            Id = messageDTO.Id ?? 0,
            Chat = messageDTO.ChatId ?? 0,
            SenderId = messageDTO.From ?? 0,
            Time = messageDTO.Date ?? 0,
            ReplyTo = messageDTO.ReplyTo ?? 0,
            Edited = messageDTO.Edited ?? false,
            Content = content,
            Status = messageDTO.Unread == false ? MessageStatus.Read : MessageStatus.Delivered
        };
    }
    public static ChatModel ConvertChat(ChatDTO chatDTO)
    {
        ProfileModel profile = new()
        {
            Name = chatDTO.Name ?? string.Empty,
            Username = chatDTO.Username ?? string.Empty,
            Avatar = Base64ToBitmapConverter.ConvertBase64(chatDTO.Photo)
        };
        if (chatDTO.IsGroup == true)
        {
            return new GroupModel()
            {
                Id = chatDTO.Id ?? 0,
                Type = ChatType.Group,
                Profile = profile,
            };
        }
        else
        {
            return new UserModel()
            {
                Id = chatDTO.Id ?? 0,
                Type = ChatType.Chat,
                Profile = profile,
            };
        }
    }
}
