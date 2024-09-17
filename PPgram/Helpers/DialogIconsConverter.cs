using Avalonia.Data.Converters;
using System;
using System.Globalization;
using PPgram.MVVM.ViewModels;

namespace PPgram.Helpers;

public class DialogIconsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DialogIcons iconState && parameter is string targetState)
        {
            return iconState.ToString() == targetState;
        }
        return false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}