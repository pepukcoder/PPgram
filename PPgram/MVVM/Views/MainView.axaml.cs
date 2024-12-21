using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PPgram.App;
using PPgram.Shared;

namespace PPgram.MVVM.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
    private async void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AppState appState = AppState.Instance;
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() )
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            IInsetsManager? im = topLevel?.InsetsManager;
            IInputPane? ip = topLevel?.InputPane;
            IFocusManager? fm = topLevel?.FocusManager;
            IStorageProvider? sp = topLevel?.StorageProvider;
            if (im != null)
            {
                im.SystemBarColor = Color.FromArgb(255, 18, 18, 18);
                im.DisplayEdgeToEdge = true;
            }
            if (ip != null)
            {
                ip.StateChanged += (sender, e) => 
                {
                    int ipheight = (int)Math.Round(ip.OccludedRect.Height, 0);
                    if (ip.State == InputPaneState.Open) ContentGrid.Margin = Thickness.Parse("0,0,0," + ipheight);
                    else if (fm != null)
                    {
                        fm.ClearFocus();
                        ContentGrid.Margin = Thickness.Parse("0,0,0,0");
                    }     
                };
            }
            if (sp != null)
            {
                IStorageFolder? folder = await sp.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);
                string? path = folder?.Path.AbsolutePath;
                if (path != null) appState.DownloadsFolder = Path.Combine(path, "PPgram");
            }
        }
        else appState.DownloadsFolder = PPPath.DesktopDownloadsFolder;
    }
}