using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Message;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PPgram.Controls.Chat;

public partial class ChatControl : UserControl
{
    private readonly ProfileState profileState = ProfileState.Instance;
    private readonly AppState appState = AppState.Instance;
    private readonly DispatcherTimer timer;
    private bool fetchThrottle = false;
    public ChatControl()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<Msg_OpenAttachFiles>(this, (r, e) => OpenFileDialog());
        timer = new() { Interval = TimeSpan.FromMilliseconds(appState.MessagesFetchDelay)};
        timer.Tick += (s, e) => { fetchThrottle = false; };
        timer.Start();
    }
    private async void OpenFileDialog()
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
        TransformedBounds? chatBounds = MessageHistory.GetTransformedBounds();
        if (chatBounds == null) return;
        Rect chatClip = chatBounds.Value.Clip;
        List<MessageModel> fetchedMessages = MessageHistory.Items.Source.OfType<MessageModel>().ToList();
        List<MessageModel> screenMessages = [];
        List<MessageModel> readMessages = [];
        // detect read messages
        for (int index = 0; index < controls.Count(); index++)
        {
            TransformedBounds? itemBounds = controls.ElementAt(index).GetTransformedBounds();
            if (itemBounds == null) return;
            Rect itemClip = itemBounds.Value.Clip;
            if (chatClip.Intersects(itemClip) && MessageHistory.Items.Source[index] is MessageModel message)
            {
                screenMessages.Add(message);
                if (message.SenderId != profileState.UserId && message.Status == MessageStatus.UnReadInvisible)
                {
                    message.Status = MessageStatus.ReadInvisible;
                    readMessages.Add(message);
                }
            }
        }        
        if (readMessages.Count > 0) WeakReferenceMessenger.Default.Send(new Msg_SendRead { messages = readMessages });
        // detect prefetch
        if (fetchedMessages.Count == 0 || fetchThrottle) return;
        int upper_index = fetchedMessages.IndexOf(screenMessages.Last());
        int lower_index = fetchedMessages.IndexOf(screenMessages.First());
        if (fetchedMessages.Count - (upper_index + 1) <= appState.MessagesFetchThreshold)
        {
            WeakReferenceMessenger.Default.Send(new Msg_FetchMessages
            {
                forward = false,
                anchor = fetchedMessages.Last(),
                index = upper_index,
            });
            fetchThrottle = true;
        }
        if (lower_index <= appState.MessagesFetchThreshold)
        {
            WeakReferenceMessenger.Default.Send(new Msg_FetchMessages
            { 
                forward = true,
                anchor = fetchedMessages.First(),
                index = lower_index,
            });
            fetchThrottle = true;
        }
    }
}
