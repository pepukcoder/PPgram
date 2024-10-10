using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace PPgram.MVVM.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() )
        {
            var im = TopLevel.GetTopLevel(this)?.InsetsManager;
            if (im != null)
            {
                im.SystemBarColor = Color.FromArgb(255, 18, 18, 18);
                im.DisplayEdgeToEdge = true;
            }
            var ip = TopLevel.GetTopLevel(this)?.InputPane;
            var fm = TopLevel.GetTopLevel(this)?.FocusManager;
            if (ip != null)
            {
                ip.StateChanged += (sender, e) => 
                {
                    int ipheight = (int)Math.Round(ip.OccludedRect.Height, 0);
                    if (ip.State == Avalonia.Controls.Platform.InputPaneState.Open)
                        ContentGrid.Margin = Thickness.Parse("0,0,0," + ipheight);
                    else if (fm != null)
                    {
                        fm.ClearFocus();
                        ContentGrid.Margin = Thickness.Parse("0,0,0,0");
                    }     
                };
            }
        }
    }
}