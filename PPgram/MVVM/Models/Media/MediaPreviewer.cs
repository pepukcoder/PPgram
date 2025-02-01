using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.MessageContent;
using PPgram.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace PPgram.MVVM.Models.Media;

internal partial class MediaPreviewer : ObservableObject
{
    [ObservableProperty]
    private bool visible;
    [ObservableProperty]
    private bool videoControlVisible;
    [ObservableProperty]
    private bool paused;
    [ObservableProperty]
    private bool fullscreen;

    [ObservableProperty]
    private uint volume = 100;

    [ObservableProperty]
    private string description = string.Empty;
    [ObservableProperty]
    private List<FileModel>? currentFiles;
    [ObservableProperty]
    private FileModel? currentFile;
    [ObservableProperty]
    private Bitmap? photo;
    [ObservableProperty]
    private int index = 1;
    private static void Reset() => WeakReferenceMessenger.Default.Send(new Msg_ResetPreviewer());
    public void Open(FileContentModel content, FileModel file)
    {
        Description = content.Text;
        CurrentFiles = content.Files.Where(file => file is VideoModel || file is PhotoModel).ToList();
        CurrentFile = file;
        Index = CurrentFiles.IndexOf(CurrentFile) + 1;
        Photo = new(CurrentFile.Path);
        Reset();
        Visible = true;
    }
    private void MoveToIndex(int index)
    {
        Index = index + 1;
        CurrentFile = CurrentFiles?[index];
        if (CurrentFile is PhotoModel) Photo = new(CurrentFile.Path);
    }
    [RelayCommand]
    private void MoveNext()
    {
        int index = CurrentFiles?.IndexOf(CurrentFile ?? new()) ?? 0;
        if (index < CurrentFiles?.Count - 1) MoveToIndex(index + 1);
        Reset();
    }
    [RelayCommand]
    private void MovePrevious()
    {
        int index = CurrentFiles?.IndexOf(CurrentFile ?? new()) ?? 0;
        if (index > 0) MoveToIndex(index - 1);
        Reset();
    }
    [RelayCommand]
    private void Close()
    {
        Visible = false;
        Paused = true;
        Description = string.Empty;
    }
}
