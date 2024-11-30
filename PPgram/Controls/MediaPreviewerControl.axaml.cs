using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Diagnostics;

namespace PPgram.Controls;

public partial class MediaPreviewerControl : UserControl
{
    public MediaPreviewerControl()
    {
        InitializeComponent();
        Zoom.DoubleTapped += (s, e) => Zoom.ResetMatrix();
    }
}