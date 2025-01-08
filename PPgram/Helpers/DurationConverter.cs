using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PPgram.Helpers;

/// <summary>
/// Converts seconds length value into h:mm:ss duration string
/// </summary>
public class DurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not uint seconds) return "???";
        TimeSpan span = TimeSpan.FromSeconds(seconds);

        if (seconds >= 3600) return String.Format("{0:D1}:{1:D2}:{2:D2}", span.Hours, span.Minutes, span.Seconds);
        return String.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}