using Android.App;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Http.Helpers;
using fiskaltrust.AndroidLauncher.Http.Hosting;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherHttpService")]
    public class MiddlewareLauncherHttpService : MiddlewareLauncherService
    {
        public override IHostFactory GetHostFactory() => new HttpHostFactory();

        public override IUrlResolver GetUrlResolver() => new RestUrlResolver();
    }
}