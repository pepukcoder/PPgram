using System;
using PPgram.Shared;
using PPgram.MVVM.Models.Item;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PPgram.MVVM.Models.MessageContent;
using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.User;

namespace PPgram.MVVM.Models.Message;

/// <summary>
/// Represents general properties of all messages, various content can be added
/// </summary>
partial class MessageModel : ChatItem
{
    public int Id { get; set; } = -1;
    public int Chat { get; set; } = -1;
    public int SenderId { get; set; } = -1;
    public int Color { get; set; }
    public long Time { get; set; }
    public int? ReplyTo { get; set; }

    [ObservableProperty]
    public partial ProfileModel Sender { get; set; } = new();
    [ObservableProperty]
    public partial ReplyModel Reply { get; set; } = new();
    [ObservableProperty]
    public partial MessageContentModel Content { get; set; } = new TextContentModel();

    [ObservableProperty]
    public partial bool Edited { get; set; }

    [ObservableProperty]
    public partial MessageRole Role { get; set; }

    [ObservableProperty]
    public partial MessageStatus Status { get; set; }
}