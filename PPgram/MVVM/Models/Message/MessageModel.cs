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
    public int Id { get; set; }
    public int Chat { get; set; }
    public int SenderId { get; set; }
    public int Color { get; set; }
    public long Time { get; set; }
    public int? ReplyTo { get; set; }
    public ProfileModel Sender { get; set; } = new();
    public ReplyModel Reply { get; set; } = new();
    public Bitmap Avatar { get; set; } = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));

    [ObservableProperty]
    private MessageContentModel content = new TextContentModel();
    [ObservableProperty]
    private bool edited;
    [ObservableProperty]
    private MessageRole role;
    [ObservableProperty]
    private MessageStatus status;
}