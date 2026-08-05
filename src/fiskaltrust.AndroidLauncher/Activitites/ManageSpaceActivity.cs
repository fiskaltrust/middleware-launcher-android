using Android.App;
using Android.OS;
using Microsoft.Maui.Controls.Embedding;

namespace fiskaltrust.AndroidLauncher.Activitites
{
    [Activity(Label = "ManageSpaceActivity", Name = "eu.fiskaltrust.androidlauncher.ManageSpaceActivity", Exported = true, Theme = "@style/Maui.SplashTheme")]
    public class ManageSpaceActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var services = IPlatformApplication.Current!.Services;
            var context = new MauiContext(services, this);
            var mauiView = new ManageSpaceView(this);
            SetContentView(mauiView.ToPlatformEmbedded(context));
        }
    }
}
