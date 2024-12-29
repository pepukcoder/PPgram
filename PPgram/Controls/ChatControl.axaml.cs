using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PPgram.MVVM.Models.File;
using System.Collections.Generic;
using System.IO;
using System;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;
using PPgram.MVVM.Models.Chat;
using Avalonia.VisualTree;
using System.Linq;
using System.Diagnostics;

namespace PPgram.Controls;

public partial class ChatControl : UserControl
{
    public ChatControl()
    {
        InitializeComponent();
        AttachButton.AddHandler(PointerPressedEvent, OpenFileDialog, RoutingStrategies.Tunnel);
        WeakReferenceMessenger.Default.Register<Msg_OpenAttachFiles>(this, (r, e) => OpenFileDialog(this, new()));
    }
    private async void OpenFileDialog(object? sender, RoutedEventArgs args)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Send files",
            AllowMultiple = true
        });
        if (files.Count >= 1 && this.DataContext is ChatModel chatModel)
        {
            List<FileModel> fileModels = [];
            foreach (IStorageFile file in files)
            {
                string absolutePath = Uri.UnescapeDataString(file.Path.AbsolutePath);
                string extension = Path.GetExtension(absolutePath);
                // assign model based on file extension
                if (PPFileExtensions.VideoExtensions.Contains(extension))
                {
                    fileModels.Add(new VideoModel
                    {
                        Name = file.Name,
                        Path = absolutePath,
                        Size = new FileInfo(absolutePath).Length,
                        Preview = new Bitmap(absolutePath).CreateScaledBitmap(new(150, 150), BitmapInterpolationMode.LowQuality)
                    });
                }
                else if (PPFileExtensions.PhotoExtensions.Contains(extension))
                {
                    fileModels.Add(new PhotoModel
                    {
                        Name = file.Name,
                        Path = absolutePath,
                        Size = new FileInfo(absolutePath).Length,
                        Preview = new Bitmap(absolutePath).CreateScaledBitmap(new(150, 150), BitmapInterpolationMode.LowQuality)
                    });
                }
                else
                {
                    fileModels.Add(new FileModel
                    {
                        Name = file.Name,
                        Path = absolutePath,
                        Size = new FileInfo(absolutePath).Length
                    });
                }
            }
            chatModel.AttachFiles(fileModels);
        }
    }
}