using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.ViewModels;
using PPgram.Shared;
using System.Collections.Generic;
using System.IO;

namespace PPgram.MVVM.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        MessageHistory.AddHandler(PointerPressedEvent, ShowFlyout, RoutingStrategies.Tunnel);
        AttachButton.AddHandler(PointerPressedEvent, OpenFileDialog, RoutingStrategies.Tunnel);
        WeakReferenceMessenger.Default.Register<Msg_OpenAttachFiles>(this, (r, e) => OpenFileDialog(this, new()));
    }
    private void ShowFlyout(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c) Flyout.ShowAttachedFlyout(c);
    }
    private async void OpenFileDialog(object? sender, RoutedEventArgs args)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files",
            AllowMultiple = true
        });
        if (files.Count >= 1 && this.DataContext is ChatViewModel chatViewModel)
        {
            List<FileModel> fileModels = [];
            foreach (IStorageFile file in files)
            {
                // TODO: Add format check to assign proper models
                fileModels.Add(new FileModel
                {
                    Name = file.Name,
                    Path = file.Path.AbsolutePath,
                    Size = new FileInfo(file.Path.AbsolutePath).Length
                });
            }
            chatViewModel.AttachFiles(fileModels);
        }
    }
}