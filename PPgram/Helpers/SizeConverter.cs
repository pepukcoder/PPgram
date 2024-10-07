using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PPgram.Helpers;

internal class SizeConverter : IValueConverter
{
    static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB"];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int size)
        {
            if (size == 0) return "0 bytes";
            if (size < 0)
                return "???";
            int i = 0;
            decimal dValue = (decimal)size;
            while (Math.Round(dValue, 1) >= 1000)
            {
                dValue /= 1000;
                i++;
            }
            return string.Format("{0:n1} {1}", dValue, SizeSuffixes[i]);
        }
        return "???";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}