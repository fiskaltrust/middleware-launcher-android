using Android.App;
using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.AndroidLauncher.Grpc.Broadcasting;
using fiskaltrust.AndroidLauncher.Grpc.Hosting;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Common.Constants;

namespace fiskaltrust.AndroidLauncher.Grpc
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherGrpcService")]
    public class MiddlewareLauncherGrpcService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => new GrpcHostFactory();

        public override IUrlResolver GetUrlResolver() => new GrpcUrlResolver();

        public override IBinder OnBind(Intent intent)
        {
            var binder = new POSProviderBinder(this);
            var stopBroadcastReceiver = new StopLauncherBroadcastReceiver();
            RegisterReceiver(stopBroadcastReceiver, new IntentFilter(BroadcastConstants.StopBroadcastName));
            return binder;
        }
    }
}