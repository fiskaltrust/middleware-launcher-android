using fiskaltrust.AndroidLauncher.Services.POSSystemApiCore;
using fiskaltrust.Api.PosSystem.Core;

namespace fiskaltrust.AndroidLauncher.Services
{    
    public static class LauncherRuntimeState
    {
        public static LocalMiddlewareLauncher? LocalMiddlewareServiceInstance { get; set; }

        public static PosSystemApiCore? PosSystemApiCore { get; set; }
        public static POSSystemApiCoreConfiguration? POSSystemApiCoreConfiguration { get; set; }
    }
}
