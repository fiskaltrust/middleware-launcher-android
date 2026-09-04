using Android.App;
using Android.OS;
using AndroidX.Core.App;
using fiskaltrust.AndroidLauncher.Enums;

namespace fiskaltrust.AndroidLauncher.Notifications
{
    /// <summary>
    /// Owns the persistent middleware state notification: channel setup, building the
    /// notification for a given launcher state, and updating it while the service runs.
    /// </summary>
    internal class LauncherNotificationHandler : ILauncherStateNotifier
    {
        public const int NotificationId = 0x66746d77;
        private const string NOTIFICATION_CHANNEL_ID = "eu.fiskaltrust.launcher.android";

        public static LauncherNotificationHandler Instance { get; } = new();

        private LauncherState _currentState = LauncherState.NotConnected;
        private string? _currentContentText;

        public void Notify(LauncherState state, string? contentText = null)
        {
            _currentState = state;
            _currentContentText = contentText;

            var manager = (NotificationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.NotificationService)!;
            manager.Notify(NotificationId, BuildNotification(state, contentText));
        }

        /// <summary>
        /// Builds the notification for the most recently notified state. Used by the
        /// service for <c>StartForeground</c>, so repeated service starts don't reset
        /// the notification to the initial state.
        /// </summary>
        public Notification BuildCurrentNotification() => BuildNotification(_currentState, _currentContentText);

        public void EnsureNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "fiskaltrust Middleware", NotificationImportance.Default)
                {
                    Description = "The fiskaltrust Middleware"
                };
                var manager = (NotificationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.NotificationService)!;
                manager.CreateNotificationChannel(channel);
            }
        }

        private static Notification BuildNotification(LauncherState state, string? contentText)
        {
            int icon = state switch
            {
                LauncherState.NotConnected => Resource.Drawable.ft_notification_notconnected,
                LauncherState.Connected => Resource.Drawable.ft_notification_connected,
                LauncherState.Error => Resource.Drawable.ft_notification_error,
                _ => throw new NotImplementedException(),
            };
            var text = state switch
            {
                LauncherState.NotConnected => "The fiskaltrust Middleware is starting. This will take a few seconds, depending on the TSE.",
                LauncherState.Connected => "The fiskaltrust Middleware is running.",
                LauncherState.Error => "An error occured in the fiskaltrust Middleware. Please restart it.",
                _ => throw new NotImplementedException(),
            };
            if (contentText != null)
                text = contentText;
            var builder = new NotificationCompat.Builder(Android.App.Application.Context, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle(Android.App.Application.Context.Resources.GetString(Resource.String.app_name))
                .SetContentText(text)
                .SetCategory(Notification.CategoryService)
                .SetSmallIcon(icon)
                .SetOngoing(true)
                .SetNotificationSilent();

            return builder.Build();
        }
    }
}
