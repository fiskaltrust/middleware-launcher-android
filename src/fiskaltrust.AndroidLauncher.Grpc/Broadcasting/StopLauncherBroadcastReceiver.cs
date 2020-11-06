using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;

namespace fiskaltrust.AndroidLauncher.Grpc.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.GrpcStopBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            LauncherBootstrapper.Teardown(context);
        }
    }
}