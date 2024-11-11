using System;
using PPgram.Shared;
using PPgram.MVVM.Models.Item;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PPgram.MVVM.Models.MessageContent;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.Message;

partial class MessageModel : ChatItem
{
    public int Id { get; set; }
    public int Chat { get; set; }
    public int SenderId { get; set; }
    public int Color { get; set; }
    public string Sender { get; set; } = string.Empty;
    public int ReplyTo { get; set; }
    public ReplyModel Reply { get; set; } = new();
    public Bitmap Avatar { get; set; } = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public MessageContentModel Content { get; set; } = new TextContentModel();
    public bool Edited { get; set; }
    public long Time { get; set; }
    [ObservableProperty]
    public MessageRole role;
    [ObservableProperty]
    public MessageStatus status;
}