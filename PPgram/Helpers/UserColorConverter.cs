using Avalonia;
using Avalonia.Styling;
using Avalonia.Data.Converters;
using System.Globalization;
using System;
using Avalonia.Media;


namespace PPgram.Helpers;

internal class UserColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int colorCode)
        {
            string brushKey = colorCode switch
            {
                1 => "UserColor1",
                2 => "UserColor2",
                3 => "UserColor3",
                4 => "UserColor4",
                5 => "UserColor5",
                6 => "UserColor6",
                _ => "UserColor0"
            };
            return Application.Current?.Resources[brushKey] ?? new SolidColorBrush(Colors.White);
        }
        return new SolidColorBrush(Colors.White);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}
