using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace PPgram.Helpers;

/// <summary>
/// Converts base64 string into bitmap
/// </summary>
public class Base64ToBitmapConverter
{
    public static Bitmap ConvertBase64(string? base64String)
    {
        try
        {
            if (base64String == null) return new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using MemoryStream ms = new(imageBytes);
            return new Bitmap(ms);
        }
        catch 
        {
            return new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/default_avatar.png", UriKind.Absolute)));
        }
    }
}


