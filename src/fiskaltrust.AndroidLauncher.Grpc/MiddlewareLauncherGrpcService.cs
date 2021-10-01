using Android.App;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Grpc.Hosting;
using fiskaltrust.AndroidLauncher.Helpers;

namespace fiskaltrust.AndroidLauncher.Grpc
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherGrpcService")]
    public class MiddlewareLauncherGrpcService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => new GrpcHostFactory();

        public override IUrlResolver GetUrlResolver() => new GrpcUrlResolver();
    }
}