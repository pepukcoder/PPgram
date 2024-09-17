using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace PPgram.Android;

[Activity(
    Label = "PPgram",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/andrlogo",
    MainLauncher = true,
    ResizeableActivity = false,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
