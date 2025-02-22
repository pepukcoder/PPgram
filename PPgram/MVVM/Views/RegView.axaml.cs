using Avalonia.Controls;

namespace PPgram.MVVM.Views;

public partial class RegView : UserControl
{
    public RegView()
    {
        InitializeComponent();
    }
    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb) tb.Text = tb.Text?.ToLower();
    }
}