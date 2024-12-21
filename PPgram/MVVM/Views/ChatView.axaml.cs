using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.ViewModels;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PPgram.MVVM.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }
}
