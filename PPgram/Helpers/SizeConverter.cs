using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PPgram.Helpers;

/// <summary>
/// Converts byte size value into prefixed UI-compatible size string
/// </summary>
internal class SizeConverter : IValueConverter
{
    static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB"];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long size || size < 0) return "???";
        if (size == 0) return "0 bytes";
        int i = 0;
        decimal dValue = (decimal)size;
        while (Math.Round(dValue, 1) >= 1000)
        {
            dValue /= 1000;
            i++;
        }
        return string.Format("{0:n1} {1}", dValue, SizeSuffixes[i]);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}