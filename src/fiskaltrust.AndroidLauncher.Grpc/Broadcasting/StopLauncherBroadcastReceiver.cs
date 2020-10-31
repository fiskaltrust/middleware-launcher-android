using Android.App;
using Android.Content;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.AndroidService;

namespace fiskaltrust.AndroidLauncher.Grpc.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "eu.fiskaltrust.androidlauncher.grpc.Stop")]
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