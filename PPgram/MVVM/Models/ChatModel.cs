using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PPgram.Shared;

namespace PPgram.MVVM.Models;

internal class ChatModel
{
    public int Id { get; set; }
    public int UnreadCount { get; set; }
    public bool Online { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public string Date { get; set; } = "00:00";
    public Bitmap Avatar { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
    public MessageStatus Status { get; set; }
    public ObservableCollection<MessageModel> Messages { get; set; } = [];
}
