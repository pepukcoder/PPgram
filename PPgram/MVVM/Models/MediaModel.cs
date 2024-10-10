using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace PPgram.MVVM.Models;

internal class MediaModel
{
    public int Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasPreview { get; set; }
    public Bitmap Preview {get; set; }  = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/image_broken.png", UriKind.Absolute)));
}