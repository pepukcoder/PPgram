using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PPgram.Helpers;

public class BoolToColorConverter(IBrush trueColor, IBrush falseColor) : IValueConverter
{
    public IBrush TrueColor { get; set; } = trueColor;
    public IBrush FalseColor { get; set; } = falseColor;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueColor : FalseColor;
        }
        return new BindingNotification(new InvalidCastException("Expected a boolean value"), BindingErrorType.Error);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Equals(value, TrueColor))
            return true;
        if (Equals(value, FalseColor))
            return false;
        return new BindingNotification(new ArgumentException("Not valid value"), BindingErrorType.Error);
    }
}