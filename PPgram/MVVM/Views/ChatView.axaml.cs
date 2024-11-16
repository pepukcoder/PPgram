using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.Custom;
using PPgram.MVVM.ViewModels;
using System.Diagnostics;

namespace PPgram.MVVM.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        MessageHistory.AddHandler(PointerPressedEvent, ShowHistoryFlyout, RoutingStrategies.Tunnel);
    }
    private void ShowHistoryFlyout(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c) Flyout.ShowAttachedFlyout(c);
    }
}