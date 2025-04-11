using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Services;

namespace fiskaltrust.AndroidLauncher.Http.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.HttpStopBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopHttpLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            MiddlewareLauncherService.Stop<MiddlewareLauncherHttpService>();
            StateProvider.Instance.SetState(State.Uninitialized);
        }
    }
}