using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.File;
using System;
using System.Collections.Generic;
using System.IO;

namespace PPgram.Controls.DialogContent;

public partial class NewGroupDialogControl : UserControl
{
    public NewGroupDialogControl()
    {
        InitializeComponent();
        picbtn.AddHandler(PointerPressedEvent, OpenFileDialog, RoutingStrategies.Tunnel);
    }
    private async void OpenFileDialog(object? sender, RoutedEventArgs args)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new() 
        { 
            Title = "Choose picture", 
            AllowMultiple = false, 
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });
        if (this.DataContext is NewGroupDialog dialog)
        {
            foreach (IStorageFile file in files)
            {
                string absolutePath = Uri.UnescapeDataString(file.Path.AbsolutePath);
                PhotoModel photo = new()
                {
                    Name = file.Name,
                    Path = absolutePath,
                    Size = new FileInfo(absolutePath).Length,
                    Preview = new Bitmap(absolutePath).CreateScaledBitmap(new(150, 150), BitmapInterpolationMode.MediumQuality)
                };
                dialog.SetPhoto(photo);
                break;
            }
        }
    }
}