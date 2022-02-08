using Android.App;
using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.AndroidLauncher.Http.Broadcasting;
using fiskaltrust.AndroidLauncher.Http.Helpers;
using fiskaltrust.AndroidLauncher.Http.Hosting;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherHttpService")]
    public class MiddlewareLauncherHttpService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => new HttpHostFactory();

        public override IUrlResolver GetUrlResolver() => new RestUrlResolver();

        public override IBinder OnBind(Intent intent)
        {
            var binder = new POSProviderBinder(this);
            var stopBroadcastReceiver = new StopLauncherBroadcastReceiver();
            RegisterReceiver(stopBroadcastReceiver, new IntentFilter(BroadcastConstants.StopBroadcastName));
            return binder;
        }
    }
}