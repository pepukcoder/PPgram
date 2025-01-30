using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;
using System;
using Avalonia.Media;


namespace PPgram.Helpers;

internal class UserColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int code && code >= 0 && code <= 21)
        {
            string brushKey = $"UserColor{code}";
            return Application.Current?.Resources[brushKey] ?? new SolidColorBrush(Colors.White);
        }
        return new SolidColorBrush(Colors.White);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}
