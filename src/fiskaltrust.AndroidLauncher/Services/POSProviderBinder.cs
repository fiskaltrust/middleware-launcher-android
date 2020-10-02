using Android.OS;
using fiskaltrust.AndroidLauncher.AndroidService;

namespace fiskaltrust.AndroidLauncher.Services
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