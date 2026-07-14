using fiskaltrust.Api.PosSystem.Core;

namespace fiskaltrust.AndroidLauncher.Services
{
    /// <summary>
    /// Process-wide runtime state shared between the foreground
    /// <see cref="AndroidService.MiddlewareLauncherService"/> and the bound
    /// <c>PosSystemAPIService</c>.
    ///
    /// This replaces the previous static fields that lived on
    /// <c>PosSystemAPIActivity</c>.
    /// </summary>
    public static class LauncherRuntimeState
    {
        public static LocalMiddlewareLauncher? LocalMiddlewareServiceInstance { get; set; }

        public static PosSystemApiCore? PosSystemApiCore { get; set; }
    }
}
