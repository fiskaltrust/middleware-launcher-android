using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Services;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Http.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.HttpStartBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StartLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var cashboxId = intent.GetStringExtra("cashboxid");
            var accessToken = intent.GetStringExtra("accesstoken");
            var isSandbox = intent.GetBooleanExtra("sandbox", false);
            var enableCloseButton = intent.GetBooleanExtra("enableCloseButton", false);
            var logLevel = Enum.TryParse(intent.GetStringExtra("loglevel"), out LogLevel level) ? level : LogLevel.Information;
            var scuParams = intent.GetScuConfigParameters();

            if (StateProvider.Instance.CurrentValue.CurrentState == State.Error)
            {
                MiddlewareLauncherService.Stop<MiddlewareLauncherHttpService>();
            }

            MiddlewareLauncherService.Start<MiddlewareLauncherHttpService>(cashboxId, accessToken, isSandbox, logLevel, scuParams, enableCloseButton);
            PowerManagerHelper.AskUserToDisableBatteryOptimization(context);
        }
    }
}
