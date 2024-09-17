using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PPgram.MVVM.Views;

public partial class RegView : UserControl
{
    public RegView()
    {
        InitializeComponent();
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender != null)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = tb.Text?.ToLower();
        }
    }
}