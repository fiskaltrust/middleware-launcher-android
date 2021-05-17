using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.StopBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StopLauncherBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler StopLauncherReceived;

        public override void OnReceive(Context context, Intent intent)
        {
            switch (intent.Action)
            {
                case BroadcastConstants.StopBroadcastName:
                    if (StopLauncherReceived != null)
                    {
                        StopLauncherReceived(this, null);
                    }
                    break;
            }
        }
    }
}