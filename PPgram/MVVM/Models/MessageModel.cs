using Avalonia.Media.Imaging;

namespace PPgram.MVVM.Models;

internal class MessageModel
{
    public int Id { get; set; }
    public int Chat { get; set; }
    public int From { get; set; }

    public string Name { get; set; } = "";
    // public SolidColorBrush NameColor { get; set; }
    public Bitmap Avatar { get; set; }

    public bool Content { get; set; }
    public string Text { get; set; } = "";
    public string Date { get; set; } = "00:00";

    public bool Reply { get; set; }
    public string ReplyName { get; set; }
    // public SolidColorBrush ReplyNameColor { get; set; }
    public string ReplyText { get; set; }

    public bool Attachment { get; set; }
    public string AttachmentHash { get; set; }
    public string AttachmentName { get; set; }
    public string AttachmentSize { get; set; }

    public bool Edited { get; set; }
    public bool First { get; set; }
    public bool Last { get; set; }
    public bool Own { get; set; }
    public bool Group { get; set; }
    public bool Delivered { get; set; }
    public bool Unread { get; set; }
}
