using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;

namespace PPgram.Controls;

public partial class MediaPreviewerControl : UserControl
{
    public MediaPreviewerControl()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<Msg_ResetPreviewer>(this, (r, e) => Zoom.ResetMatrix());
        Zoom.DoubleTapped += (s, e) => Zoom.ResetMatrix();
    }
}