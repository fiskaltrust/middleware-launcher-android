using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common;

namespace fiskaltrust.AndroidLauncher
{
    //[BroadcastReceiver(Enabled = true, Name = "eu.fiskaltrust.AndroidLauncher.Start")]
    //[IntentFilter(new[] { "eu.fiskaltrust.AndroidLauncher.Start" })]
    //public class StartLauncherBroadcastReceiver : BroadcastReceiver
    //{
    //    public override void OnReceive(Context context, Intent intent)
    //    {
    //        var cashboxId = intent.GetStringExtra("cashboxid");
    //        var accessToken = intent.GetStringExtra("accesstoken");
    //        MiddlewareLauncherService.Start(ServiceConnectionProvider.GetConnection(), cashboxId, accessToken);
    //    }
    //}

    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { "dk.ostebaronen.droid.TEST" })]
    public class MyTestReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            // Do stuff here
        }
    }
}