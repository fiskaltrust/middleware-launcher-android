using Android.Content;
using Android.OS;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    public static class PowerManagerHelper
    {
        public static void AskUserToDisableBatteryOptimization(Context context)
        {
            string packageName = context.PackageName;
            var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);

            if (!pm.IsIgnoringBatteryOptimizations(packageName))
            {
                var powerIntent = new Intent();
                powerIntent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                powerIntent.SetData(Android.Net.Uri.Parse("package:" + packageName));
                powerIntent.SetFlags(ActivityFlags.NewTask);
                context.StartActivity(powerIntent);
            }
        }
    }
}