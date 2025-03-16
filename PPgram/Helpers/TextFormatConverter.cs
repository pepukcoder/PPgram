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
    /*
     * Matches all formatting patterns in matches,
     * formatted text itself can be accessed with capture groups:
     * 1 - **bold**
     * 2 - @mention
     * 3 - __italic__
     * 4 - ***bold+italic***
     */
    private static readonly string regex = @"\*{2}([^*]+?)\*{2}|@(\w+)|_{2}([^*]+?)_{2}|\*{3}([^*]+?)\*{3}";
    private static readonly Regex Formatting = new(regex, RegexOptions.Compiled);
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string raw || String.IsNullOrEmpty(raw)) return new InlineCollection();
        MatchCollection matches = Formatting.Matches(raw);
        InlineCollection inlines = new() { Capacity = matches.Count * 2};
        int lastIndex = 0;
        foreach (Match match in matches)
        {
            // add text before match
            if (match.Index > lastIndex) inlines.Add(new Run(raw[lastIndex..match.Index]));
            // add corresponding inline
            for (int i = 1; i < match.Groups.Count; i++)
            {
                Group group = match.Groups[i];
                if (!group.Success) continue;
                inlines.Add(i switch
                {
                    1 => new Run(group.Value) { FontWeight = FontWeight.Bold }, // **bold**
                    3 => new Run(group.Value) { FontStyle = FontStyle.Italic }, // __italic__
                    4 => new Run(group.Value) { FontWeight = FontWeight.Bold, FontStyle = FontStyle.Italic }, // ***bold+italic***
                    _ => throw new InvalidOperationException("Unexpected match")
                });
                break;
            }
            // move to next chunk
            lastIndex = match.Index + match.Length;
        }
        // add remaining text
        if (lastIndex < raw.Length) inlines.Add(new Run(raw[lastIndex..]));
        return inlines;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
