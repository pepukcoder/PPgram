using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Message;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PPgram.Controls.Chat;

public partial class ChatControl : UserControl
{
    private readonly ProfileState profileState = ProfileState.Instance;
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
    private void ChatScrolled(object? sender, ScrollChangedEventArgs e)
    {
        IEnumerable<Control> controls = MessageHistory.GetRealizedContainers();
        TransformedBounds? globaltransform = MessageHistory.GetTransformedBounds();
        if (globaltransform == null) return;
        Rect viewport = globaltransform.Value.Clip;
        List<MessageModel> readmessages = [];
        for (int index = 0; index < controls.Count(); index++)
        {
            TransformedBounds? bounds = controls.ElementAt(index).GetTransformedBounds();
            if (bounds == null) return;
            Rect clip = bounds.Value.Clip;
            if (viewport.Intersects(clip) && MessageHistory.Items.Source[index] is MessageModel message
                && message.SenderId != profileState.UserId && message.Status == MessageStatus.UnReadInvisible)
            {
                message.Status = MessageStatus.ReadInvisible;
                readmessages.Add(message);
            }
        }
        if (readmessages.Count > 0) WeakReferenceMessenger.Default.Send(new Msg_SendRead { messages = readmessages });
    }
}
