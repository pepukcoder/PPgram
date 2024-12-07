using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace PPgram.Android;

[Activity(
    Label = "PPgram",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/andrlogo",
    ResizeableActivity = false,
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<PPApp>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
