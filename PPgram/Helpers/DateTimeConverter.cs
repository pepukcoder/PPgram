using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PPgram.Helpers;

/// <summary>
/// Converts unix time value into H:mm format and adjusts it to user's local time
/// </summary>
/// <remarks>
/// Use ConverterParameter Date in chatlist to show weekdays for old chats
/// </remarks>
internal class DateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is long unixtime)
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixtime).LocalDateTime;
            if (parameter is string mode && mode == "Date" && dateTime.Date != DateTime.Today)
            {
                return dateTime.ToString("ddd");
            }
            return dateTime.ToString("H:mm");
        }
        return "0:00";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}
