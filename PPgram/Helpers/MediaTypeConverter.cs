using Avalonia.Data.Converters;
using System;
using System.Globalization;
using PPgram.MVVM.Models;

namespace PPgram.Helpers;

public class MediaTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MediaType mediaType && parameter is string targetStatus)
        {
            return mediaType.ToString() == targetStatus;
        }
        return false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}