using Avalonia.Data.Converters;
using System;
using System.Globalization;
using PPgram.MVVM.Models;

namespace PPgram.Helpers;

public class MessageStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageStatus messageStatus && parameter is string targetStatus)
        {
            return messageStatus.ToString() == targetStatus;
        }
        return false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}