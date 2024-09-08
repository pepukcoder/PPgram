using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace PPgram_desktop.Core;

internal class Base64ToBitmapConverter
{
    public static Bitmap ConvertBase64(string base64String)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            Bitmap bitmapImage;
            using (MemoryStream ms = new(imageBytes))
            {
                bitmapImage = new Bitmap(ms);
            }
            return bitmapImage;
        }
        catch 
        {
            return new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/image_broken.png", UriKind.Absolute)));
        }
    }
}


