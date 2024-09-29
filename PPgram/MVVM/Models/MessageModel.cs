using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace PPgram.MVVM.Models;

internal class MessageModel
{
    public int Id { get; set; }
    public int Chat { get; set; }
    public int From { get; set; }
    public string Name { get; set; } = "UserName";
    // public SolidColorBrush NameColor { get; set; }
    public Bitmap Avatar { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public string Text { get; set; }
    public string Date { get; set; } = "00:00";
    public string ReplyName { get; set; }
    // public SolidColorBrush ReplyNameColor { get; set; }
    public string ReplyText { get; set; }
    public string AttachmentHash { get; set; }
    public string AttachmentName { get; set; }
    public string AttachmentSize { get; set; } = "0 MB";
    public bool Edited { get; set; }
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; }
}
public enum MessageType
{
    User,
    UserFirst,
    Own,
    OwnFirst,
    Group,
    GroupFirst,
    GroupLast,
    Date
}
public enum MessageStatus
{
    None,
    Sending,
    Delivered,
    Read,
    Error
}