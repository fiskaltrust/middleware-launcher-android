using Android.App;
using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.Helpers;
using Android.Content.PM;
using fiskaltrust.AndroidLauncher.AndroidService;

namespace fiskaltrust.AndroidLauncher.Grpc
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncher", ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class DefaultMiddlewareLauncherService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => null;

        public override IUrlResolver GetUrlResolver() => null;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }
}