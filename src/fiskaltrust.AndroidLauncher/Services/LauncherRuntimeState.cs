using fiskaltrust.Api.PosSystem.Core;

namespace fiskaltrust.AndroidLauncher.Services
{    
    public static class LauncherRuntimeState
    {
        public static LocalMiddlewareLauncher? LocalMiddlewareServiceInstance { get; set; }

        public static PosSystemApiCore? PosSystemApiCore { get; set; }

        public static Task? StartupTask { get; set; }
    }
}
