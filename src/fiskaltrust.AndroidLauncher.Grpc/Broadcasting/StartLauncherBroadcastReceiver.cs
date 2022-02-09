using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Grpc.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.GrpcStartBroadcastName)]
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
            MiddlewareLauncherService.Start<MiddlewareLauncherGrpcService>(cashboxId, accessToken, isSandbox, logLevel, scuParams, enableCloseButton);
        }
    }
}
