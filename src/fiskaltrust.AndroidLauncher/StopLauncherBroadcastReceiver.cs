//using Android.App;
//using Android.Content;
//using fiskaltrust.AndroidLauncher.Common;

//namespace fiskaltrust.AndroidLauncher
//{
//    [BroadcastReceiver(Enabled = true, Name = "eu.fiskaltrust.AndroidLauncher.Stop")]
//    [IntentFilter(new[] { "eu.fiskaltrust.AndroidLauncher.Stop" })]
//    public class StopLauncherBroadcastReceiver : BroadcastReceiver
//    {
//        public override void OnReceive(Context context, Intent intent)
//        {
//            MiddlewareLauncherService.Stop(ServiceConnectionProvider.GetConnection());
//        }
//    }
//}