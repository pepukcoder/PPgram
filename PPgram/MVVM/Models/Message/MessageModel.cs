using System;
using System.Collections.ObjectModel;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Message;

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
    public bool Edited { get; set; }
    public MediaType MediaType { get; set; }
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; }
}