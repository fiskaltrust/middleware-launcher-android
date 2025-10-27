using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace fiskaltrust.AndroidLauncher;

[Activity(Label = "@string/app_name", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[Register("eu.fiskaltrust.androidlauncher.MainActivity")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            SQLitePCL.Batteries_V2.Init();
        }
        catch(Exception ex)
        {
            
        }
        base.OnCreate(savedInstanceState);
    }
}
