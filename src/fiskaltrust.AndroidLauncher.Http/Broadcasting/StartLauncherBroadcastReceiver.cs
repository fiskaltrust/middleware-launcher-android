using Android.App;
using Android.Content;
using Android.OS;
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

            using var bundle = new Bundle();
            bundle.PutString("cashboxid", cashboxId);
            bundle.PutString("accesstoken", accessToken);
            bundle.PutBoolean("sandbox", isSandbox);
            bundle.PutString("loglevel", logLevel.ToString());
            bundle.PutBoolean("enableCloseButton", enableCloseButton);
            foreach (var extra in scuParams)
            {
                bundle.PutString(extra.Key, extra.Value.ToString());
            }

            using var alarmIntent = new Intent(Application.Context, typeof(HttpAlarmReceiver));
            alarmIntent.PutExtras(bundle);

            var pending = PendingIntent.GetBroadcast(Application.Context, 0, alarmIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var alarmManager = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
            alarmManager.SetExact(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 100, pending);
        }
    }

    [BroadcastReceiver]
    public class HttpAlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var cashboxId = intent.GetStringExtra("cashboxid");
            var accessToken = intent.GetStringExtra("accesstoken");
            var isSandbox = intent.GetBooleanExtra("sandbox", false);
            var enableCloseButton = intent.GetBooleanExtra("enableCloseButton", false);
            var logLevel = Enum.TryParse(intent.GetStringExtra("loglevel"), out LogLevel level) ? level : LogLevel.Information;
            var scuParams = intent.GetScuConfigParameters();

            MiddlewareLauncherService.Start<MiddlewareLauncherHttpService>(cashboxId, accessToken, isSandbox, logLevel, scuParams, enableCloseButton);
            PowerManagerHelper.AskUserToDisableBatteryOptimization(context);
        }
    }
}
