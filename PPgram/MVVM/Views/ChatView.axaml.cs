using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.ViewModels;
using PPgram.Shared;
using System;
using System.Collections.Generic;
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
                var absolutePath = Uri.UnescapeDataString(file.Path.AbsolutePath);

                string[] parts = file.Name.Split('.');
                // check extension
                switch (parts[^1].ToLower().Trim())
                {
                    // TODO: Add more supported extensions
                    case "mp4":
                        fileModels.Add(new VideoModel
                        {
                            Name = file.Name,
                            Path = absolutePath,
                            Size = new FileInfo(absolutePath).Length,
                            Preview = new Bitmap(AssetLoader.Open(new("avares://PPgram/Assets/image_broken.png", UriKind.Absolute)))
                        });
                        break;
                    case "jpeg":
                    case "jpg":
                    case "png":
                        // handle photos
                        fileModels.Add(new PhotoModel
                        {
                            Name = file.Name,
                            Path = absolutePath,
                            Size = new FileInfo(absolutePath).Length,
                            Preview = new Bitmap(absolutePath).CreateScaledBitmap(new(150, 150), BitmapInterpolationMode.LowQuality)
                        });
                        break;
                    default:
                        // handle files
                        fileModels.Add(new FileModel
                        {
                            Name = file.Name,
                            Path = absolutePath,
                            Size = new FileInfo(absolutePath).Length
                        });
                        break;
                }
            }
            chatViewModel.AttachFiles(fileModels);
        }
    }
}
