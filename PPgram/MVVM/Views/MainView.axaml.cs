using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PPgram.App;

namespace PPgram.MVVM.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
    private void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        IStorageProvider? sp = topLevel?.StorageProvider;
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() )
        {
            IInsetsManager? im = topLevel?.InsetsManager;
            IInputPane? ip = topLevel?.InputPane;
            IFocusManager? fm = topLevel?.FocusManager;
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
                    if (ip.State == InputPaneState.Open)
                        ContentGrid.Margin = Thickness.Parse("0,0,0," + ipheight);
                    else if (fm != null)
                    {
                        fm.ClearFocus();
                        ContentGrid.Margin = Thickness.Parse("0,0,0,0");
                    }     
                };
            }
            
        }
        if (sp != null)
        {
            var folder = sp.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads).GetAwaiter().GetResult();
            var path = folder?.Path.AbsolutePath;
            if (path != null)
            {
                AppState appState = AppState.Instance;
                appState.DownloadsFolder = Path.Combine(path, "PPgram");
                Debug.WriteLine(appState.DownloadsFolder);
            }
        }
    }
}