using fiskaltrust.AndroidLauncher.Enums;

namespace fiskaltrust.AndroidLauncher.Notifications
{
    /// <summary>
    /// Reports middleware launcher state changes to the user.
    /// </summary>
    internal interface ILauncherStateNotifier
    {
        /// <param name="state">The new launcher state.</param>
        /// <param name="contentText">Optional text overriding the default message for the state.</param>
        void Notify(LauncherState state, string? contentText = null);
    }
}
