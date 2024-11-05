using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PPgram.Helpers;

/// <summary>
/// Checks if model property value matches binding ConverterParameter
/// </summary>
public class EnumMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value?.ToString() is string current && parameter is string target)
        {
            return current == target;
        }
        return false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
