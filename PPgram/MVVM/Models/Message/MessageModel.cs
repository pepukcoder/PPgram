using System;
using System.Collections.ObjectModel;
using PPgram.Shared;
using PPgram.MVVM.Models.Item;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PPgram.MVVM.Models.Message;

internal class MessageModel : ChatItem
{
    public int Id { get; set; }
    public int Chat { get; set; }
    public int SenderId { get; set; }
    public int Color { get; set; }
    public string Sender { get; set; } = string.Empty;
    public ReplyModel Reply { get; set; } = new();
    public Bitmap Avatar { get; set; } = new(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public bool Edited { get; set; }
    public long Time { get; set; }
    public MessageRole Role { get; set; }
    public MessageStatus Status { get; set; }
}