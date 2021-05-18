using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.StopLauncherBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopLauncherBroadcastReceiver : BroadcastReceiver
    {
        public delegate void StopLauncherReceivedEventHandler();
        public event StopLauncherReceivedEventHandler StopLauncherReceived;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != BroadcastConstants.StopLauncherBroadcastName) { return; }

            LauncherBootstrapper.Teardown(context);

            StopLauncherReceived?.Invoke();
        }
    }
}