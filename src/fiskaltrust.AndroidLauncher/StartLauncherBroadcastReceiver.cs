using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "eu.fiskaltrust.androidlauncher.Start")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StartLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var cashboxId = intent.GetStringExtra("cashboxid");
            var accessToken = intent.GetStringExtra("accesstoken");

            Toast.MakeText(context, $"Starting fiskaltrust Middleware with cashbox '{cashboxId}'. Initializing might take up to 45 seconds, depending on the TSE.", ToastLength.Long).Show();
            MiddlewareLauncherService.Start(ServiceConnectionProvider.GetConnection(), cashboxId, accessToken);

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                // Call once to initialize Middleware. Will be replaced with a cleaner approach soon.
                await ServiceConnectionProvider.GetConnection().GetPOSAsync();
                System.Console.WriteLine();

                new Handler(Looper.MainLooper).Post(() => Toast.MakeText(context, $"Successfully started the fiskaltrust Middleware.", ToastLength.Long).Show());
            });
        }
    }
}
