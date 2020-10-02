using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Enums;
using fiskaltrust.AndroidLauncher.Exceptions;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.AndroidService.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "eu.fiskaltrust.androidlauncher.Start")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StartLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var cashboxId = intent.GetStringExtra("cashboxid");
            var accessToken = intent.GetStringExtra("accesstoken");
            var isSandbox = intent.GetBooleanExtra("sandbox", false);

            Toast.MakeText(context, $"Starting fiskaltrust Middleware with cashbox '{cashboxId}' (Sandbox: {isSandbox}). Initializing might take up to 45 seconds, depending on the TSE.", ToastLength.Long).Show();
            MiddlewareLauncherService.Start(ServiceConnectionProvider.GetConnection(), cashboxId, accessToken, isSandbox);
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000);

                    // Call once to initialize Middleware. Will be replaced with a cleaner approach soon.
                    await ServiceConnectionProvider.GetConnection().GetPOSAsync();

                    new Handler(Looper.MainLooper).Post(() => Toast.MakeText(context, $"Successfully started the fiskaltrust Middleware.", ToastLength.Long).Show());
                    MiddlewareLauncherService.SetState(LauncherState.Connected);
                }
                catch (System.Exception ex)
                {
                    Log.Error("fiskaltrust.AndroidLauncher", ex.ToString());

                    if (ex is RemountRequiredException remountRequiredEx)
                        MiddlewareLauncherService.SetState(LauncherState.Error, remountRequiredEx.Message);
                    else if (ex is ConfigurationNotFoundException confNotFoundEx)
                        MiddlewareLauncherService.SetState(LauncherState.Error, confNotFoundEx.Message);
                    else
                        MiddlewareLauncherService.SetState(LauncherState.Error);
                }
            });
        }
    }
}
