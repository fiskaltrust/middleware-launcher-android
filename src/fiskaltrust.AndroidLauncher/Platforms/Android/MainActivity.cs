using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
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
        ApplySystemBarColors();
    }

    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        ApplySystemBarColors();
    }

    private void ApplySystemBarColors()
    {
        if (Window == null || Microsoft.Maui.Controls.Application.Current == null) return;

        var dark = (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
        var color = ((Microsoft.Maui.Graphics.Color)Microsoft.Maui.Controls.Application.Current.Resources[dark ? "FtWhiteNight" : "FtWhite"]).ToPlatform();

        Window.SetStatusBarColor(color);
        Window.SetNavigationBarColor(color);

        var insets = WindowCompat.GetInsetsController(Window, Window.DecorView);
        insets.AppearanceLightStatusBars = !dark;
        insets.AppearanceLightNavigationBars = !dark;
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
