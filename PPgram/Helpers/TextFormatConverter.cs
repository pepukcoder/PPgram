using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PPgram.Helpers;

internal partial class TextFormatConverter : IValueConverter
{
    [GeneratedRegex(@"/@(\w+)|\*{3}([^*]+?)\*{3}|\*{2}([^*]+?)\*{2}|\\_([^*]+?)\\_/gm")]
    private static partial Regex Formatting();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        InlineCollection inlines = [];
        if (value is not string raw || String.IsNullOrEmpty(raw)) return inlines;
        // parse format chunks
        MatchCollection matches = Formatting().Matches(raw);
        
        // placeholder
        inlines.Add(new Run() { Text = raw });
        return inlines;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
