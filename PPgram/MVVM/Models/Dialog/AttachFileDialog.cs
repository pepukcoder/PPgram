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
    [ObservableProperty]
    private string text = string.Empty;

    [RelayCommand]
    private void SendFiles()
    {
        WeakReferenceMessenger.Default.Send(new Msg_SendAttachFiles { description = Text });
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
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
    [RelayCommand]
    private void Close()
    {
        Files.Clear();
        WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
    }
}
