using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using PPgram.MVVM.ViewModels;
using PPgram.MVVM.Views;
using PPgram.Behaviours;

namespace PPgram;

public partial class PPApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Animation.RegisterCustomAnimator<string, CustomStringAnimator>();
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}