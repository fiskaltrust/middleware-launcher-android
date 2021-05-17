using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;
using System;

namespace fiskaltrust.AndroidLauncher.Http.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.HttpStopBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopLauncherBroadcastReceiver : BroadcastReceiver
    {
        public delegate void OnStopLauncherReceived();
        public event OnStopLauncherReceived StopLauncherReceived;

        public override void OnReceive(Context context, Intent intent)
        {
            if (StopLauncherReceived != null)
            {
                StopLauncherReceived();
            }

            LauncherBootstrapper.Teardown(context);
        }

    }
}