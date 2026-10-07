using Android.App;
using Android.OS;
using Microsoft.Maui.Controls.Embedding;

namespace fiskaltrust.AndroidLauncher.Activitites
{
    [Activity(Label = "ManageSpaceActivity", Name = "eu.fiskaltrust.androidlauncher.ManageSpaceActivity", Exported = true, Theme = "@style/ManageSpaceOverlayTheme")]
    public class ManageSpaceActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            (Microsoft.Maui.Controls.Application.Current as IApplication)?.ThemeChanged();
            var services = IPlatformApplication.Current!.Services;
            var context = new MauiContext(services, this);
            var mauiView = new ManageSpaceView(this);
            SetContentView(mauiView.ToPlatformEmbedded(context));
        }
    }
}
