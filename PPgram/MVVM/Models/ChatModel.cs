using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;

namespace PPgram.MVVM.Models;

internal class ChatModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public Bitmap Avatar { get; set; }
    public string LastMessage { get; set; }
    public ObservableCollection<MessageModel> Messages { get; set; } = [];
}
