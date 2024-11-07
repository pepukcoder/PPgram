using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace PPgram.MVVM.Models.File;

internal class MediaFileModel : FileModel
{
    public Bitmap Preview { get; set; } = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/image_broken.png", UriKind.Absolute)));
}
