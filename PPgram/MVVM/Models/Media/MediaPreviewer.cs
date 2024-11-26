using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.Models.Media;

internal partial class MediaPreviewer : ObservableObject
{
    [ObservableProperty]
    private bool visible;
}
