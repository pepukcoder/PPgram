using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models;

internal class MessageModel
{
    public int Id { get; set; }
    public int Chat { get; set; }
    public int From { get; set; }
    public string Name { get; set; } = "PPgram User";
    // public SolidColorBrush NameColor { get; set; }
    public Bitmap Avatar { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public string Text { get; set; } = string.Empty;
    public string Date { get; set; } = "00:00";
    public string ReplyName { get; set; } = string.Empty;
    // public SolidColorBrush ReplyNameColor { get; set; }
    public string ReplyText { get; set; } = string.Empty;
    public ObservableCollection<MediaModel> Media { get; set; } = [];
    public MediaType MediaType { get; set; }
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
    GroupSingle,
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
public enum MediaType
{
    None,
    Images,
    Files,
}