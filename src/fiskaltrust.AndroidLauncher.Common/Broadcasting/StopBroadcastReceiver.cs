using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.StopBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopBroadcastReceiver : BroadcastReceiver
    {
        public delegate void OnStopLauncherReceived();
        public event OnStopLauncherReceived StopLauncherReceived;

        public override void OnReceive(Context context, Intent intent)
        {
            if(intent.Action != BroadcastConstants.StopBroadcastName) { return; }
            if (StopLauncherReceived != null)
            {
                StopLauncherReceived();
            }
        }
    }
}