using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Http.Broadcasting;
using fiskaltrust.AndroidLauncher.Http.Helpers;
using fiskaltrust.AndroidLauncher.Http.Hosting;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherHttpService", ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class MiddlewareLauncherHttpService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => new HttpHostFactory();

        public override IUrlResolver GetUrlResolver() => new RestUrlResolver();

        public override IBinder OnBind(Intent intent)
        {
            var stopBroadcastReceiver = new StopLauncherBroadcastReceiver();
            // Explicit cast of flags because of: https://github.com/xamarin/xamarin-android/issues/7503
            RegisterReceiver(stopBroadcastReceiver, new IntentFilter(BroadcastConstants.StopBroadcastName), (ActivityFlags) ReceiverFlags.Exported);
            return null;
        }
    }
}