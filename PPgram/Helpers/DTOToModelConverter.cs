using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Net.DTO;
using PPgram.Shared;
using System.Collections.Generic;

namespace PPgram.Helpers;

internal class DTOToModelConverter
{
    public static MessageModel ConvertMessage(MessageDTO messageDTO, ChatModel chat)
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
            foreach (FileModel file in files) 
            { 
                WeakReferenceMessenger.Default.Send(new Msg_DownloadFile{ file = file, meta = true });
                if (file.Hash != null && FSManager.IsHashed(file.Hash)) file.Status = FileStatus.Loaded;
            }
        }
        else
        {
            content = new TextContentModel()
            {
                Text = messageDTO.Text ?? string.Empty
            };
        }
        ProfileModel sender;
        if (chat is UserModel && chat.Profile != null) sender = chat.Profile;
        else sender = new();
        return new MessageModel
        {
            Id = messageDTO.Id ?? 0,
            Chat = messageDTO.ChatId ?? 0,
            SenderId = messageDTO.From ?? 0,
            Sender = sender,
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
                Profile = profile,
            };
        }
        else
        {
            return new UserModel()
            {
                Id = chatDTO.Id ?? 0,
                Profile = profile,
            };
        }
    }
}
