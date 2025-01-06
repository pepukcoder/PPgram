using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.File;
using PPgram.Shared;
using System.Collections.ObjectModel;

namespace PPgram.MVVM.Models.Dialog;

internal partial class AttachFileDialog : Dialog
{
    [ObservableProperty]
    private ObservableCollection<FileModel> files = [];

    [RelayCommand]
    private void Close()
    {
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
    }
    [RelayCommand]
    private void Clear()
    {
        Files.Clear();
        Close();
    }
    [RelayCommand]
    private void AddFiles()
    {
        WeakReferenceMessenger.Default.Send(new Msg_OpenAttachFiles());
    }
    [RelayCommand]
    private void RemoveFile(FileModel file)
    {
        Files.Remove(file);
        if (Files.Count == 0) Close();
    }
}
