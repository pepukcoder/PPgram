using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
using System.Data.Entity.Core.Objects;
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
    private double oldoffset = -1;
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
        // prevent horizontal resize misdetection
        if (e.ExtentDelta.X != 0) return;
        // preserve scroll offset
        if (sender is ListBox box && box.Scroll is IScrollable sw)
        {
            if (oldoffset == -1)
            {
                sw.Offset = new(0, sw.Extent.Height - sw.Viewport.Height);
                oldoffset = sw.Offset.Y;
                fetchThrottle = true;
            }
            if (e.ExtentDelta.Y > 0)
            {
                if (oldoffset < sw.Offset.Y) sw.Offset = new(0, sw.Offset.Y - e.ExtentDelta.Y);
                else sw.Offset = new(0, sw.Offset.Y + e.ExtentDelta.Y);
                fetchThrottle = true;
            }
            oldoffset = sw.Offset.Y;
        }
        // get rendered messages and listbox bounds
        IEnumerable<Control> controls = MessageHistory.GetRealizedContainers();
        TransformedBounds? chatBounds = MessageHistory.GetTransformedBounds();
        if (chatBounds == null) return;
        Rect chatClip = chatBounds.Value.Clip;
        // message detection
        List<MessageModel> fetchedMessages = MessageHistory.Items.Source.OfType<MessageModel>().ToList();
        List<MessageModel> screenMessages = [];
        List<MessageModel> readMessages = [];
        for (int index = 0; index < controls.Count(); index++)
        {
            TransformedBounds? itemBounds = controls.ElementAt(index).GetTransformedBounds();
            if (itemBounds == null) return;
            Rect itemClip = itemBounds.Value.Clip;
            // detect screen messages
            if (chatClip.Intersects(itemClip) && MessageHistory.Items.Source[index] is MessageModel message)
            {
                screenMessages.Add(message);
                // detect read messages
                if (message.SenderId != profileState.UserId && message.Status == MessageStatus.UnReadInvisible)
                {
                    message.Status = MessageStatus.ReadInvisible;
                    readMessages.Add(message);
                }
            }
        }
        if (readMessages.Count > 0) WeakReferenceMessenger.Default.Send(new Msg_SendRead { messages = readMessages });
        // prefetch detection
        if (fetchedMessages.Count == 0 || fetchThrottle) return;
        int upper_index = fetchedMessages.IndexOf(screenMessages.Last());
        int lower_index = fetchedMessages.IndexOf(screenMessages.First());
        // check for upper fetch
        if (fetchedMessages.Count - (upper_index + 1) <= appState.MessagesFetchThreshold && !fetchThrottle)
        {
            WeakReferenceMessenger.Default.Send(new Msg_FetchMessages
            {
                forward = false,
                anchor = fetchedMessages.Last(),
                index = upper_index,
            });
            fetchThrottle = true;
        }
        // check for lower fetch
        if (lower_index <= appState.MessagesFetchThreshold && !fetchThrottle)
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
