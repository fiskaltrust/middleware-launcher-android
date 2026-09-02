using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using fiskaltrust.AndroidLauncher.Helpers;

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

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == ActivityResultBridge.PickFolderRequestCode)
        {
            ActivityResultBridge.PendingPickFolderResult?.TrySetResult(resultCode == Result.Ok ? data : null);
        }
    }
}
