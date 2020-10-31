using Android.App;
using Android.Content;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.AndroidService;

namespace fiskaltrust.AndroidLauncher.Http.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "eu.fiskaltrust.androidlauncher.http.Stop")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            MiddlewareLauncherService.Stop(ServiceConnectionProvider.GetConnection());
            Toast.MakeText(context, $"fiskaltrust Middleware stopped.", ToastLength.Long).Show();
        }
    }
}