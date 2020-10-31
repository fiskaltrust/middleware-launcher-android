using Android.OS;
using fiskaltrust.AndroidLauncher.Common.AndroidService;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    public class POSProviderBinder : Binder
    {
        public POSProviderBinder(MiddlewareLauncherService service)
        {
            Service = service;
        }

        public MiddlewareLauncherService Service { get; }
    }
}