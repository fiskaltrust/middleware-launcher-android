using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Services;

namespace fiskaltrust.AndroidLauncher.Grpc.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.GrpcStopBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopGrpcLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            MiddlewareLauncherService.Stop<MiddlewareLauncherGrpcService>();
            StateProvider.Instance.SetState(State.Uninitialized);
        }
    }
}