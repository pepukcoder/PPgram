using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Xaml.Interactions.Custom;
using PPgram.MVVM.ViewModels;
using System.Diagnostics;
using System.IO;

namespace PPgram.MVVM.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        MessageHistory.AddHandler(PointerPressedEvent, ShowFlyout, RoutingStrategies.Tunnel);
        AttachButton.AddHandler(PointerPressedEvent, OpenFileDialog, RoutingStrategies.Tunnel);
    }
    private void ShowFlyout(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c) Flyout.ShowAttachedFlyout(c);
    }
    private async void OpenFileDialog(object? sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files",
            AllowMultiple = true
        });
        if (files.Count >= 1 && this.DataContext is ChatViewModel chatViewModel)
        {
            // send opened file paths to viewmodel
        }
    }
}