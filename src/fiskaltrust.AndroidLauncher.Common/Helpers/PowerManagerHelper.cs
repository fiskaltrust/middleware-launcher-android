using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using fiskaltrust.AndroidLauncher.Common.Activitites;
using Org.BouncyCastle.Ocsp;

namespace fiskaltrust.AndroidLauncher.Common.Helpers
{
    public static class PowerManagerHelper
    {
        public static void AskUserToDisableBatteryOptimization(Context context)
        {
            string packageName = context.PackageName;
            var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);

            if (!IsIgnoringBatteryOptimizations(context))
            {
                var powerIntent = new Intent();
                powerIntent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                powerIntent.SetData(Android.Net.Uri.Parse("package:" + packageName));
                powerIntent.SetFlags(ActivityFlags.NewTask);
                context.StartActivity(powerIntent);
            }
        }

        public static bool IsIgnoringBatteryOptimizations(Context context)
        {
            string packageName = context.PackageName;
            var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);

            return pm.IsIgnoringBatteryOptimizations(packageName);
        }
    }
}