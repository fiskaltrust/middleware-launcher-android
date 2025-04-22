using Android.App;
using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Grpc.Broadcasting;
using fiskaltrust.AndroidLauncher.Grpc.Hosting;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Constants;
using Android.Content.PM;

namespace fiskaltrust.AndroidLauncher.Grpc
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherGrpcService", ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class MiddlewareLauncherGrpcService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => new GrpcHostFactory();

        public override IUrlResolver GetUrlResolver() => new GrpcUrlResolver();

        public override IBinder OnBind(Intent intent)
        {
            var stopBroadcastReceiver = new StopGrpcLauncherBroadcastReceiver();
            // Explicit cast of flags because of: https://github.com/xamarin/xamarin-android/issues/7503
            RegisterReceiver(stopBroadcastReceiver, new IntentFilter(BroadcastConstants.StopBroadcastName), (ActivityFlags)ReceiverFlags.Exported);
            return null;
        }
    }
}