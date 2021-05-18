using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using fiskaltrust.AndroidLauncher.Common.Broadcasting;
using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.ifPOS.v1;
using Java.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.AndroidService
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherService")]
    public class MiddlewareLauncherService : Service, IPOSProvider
    {
        private const int NOTIFICATION_ID = 0x66746d77;
        private const string NOTIFICATION_CHANNEL_ID = "eu.fiskaltrust.launcher.android";
        private IPOSProvider _posProvider;
        private StopBroadcastReceiver _stopBroadcastReceiver;

        public IBinder Binder { get; private set; }

        public override IBinder OnBind(Intent intent)
        {
            var cashboxIdString = intent.GetStringExtra("cashboxid");
            var accesstoken = intent.GetStringExtra("accesstoken");
            var isSandbox = intent.GetBooleanExtra("sandbox", false);
            var logLevel = Enum.TryParse(intent.GetStringExtra("loglevel"), out LogLevel level) ? level : LogLevel.Information;
            var scuParams = intent.GetScuConfigParameters(removePrefix: true);

            if (string.IsNullOrEmpty(cashboxIdString) || !Guid.TryParse(cashboxIdString, out var cashboxId))
            {
                throw new ArgumentException("The extra 'cashboxid' needs to be set in this intent.", "cashboxid");
            }
            if (string.IsNullOrEmpty(accesstoken))
            {
                throw new ArgumentException("The extra 'accesstoken' needs to be set in this intent.", "accesstoken");
            }

            _posProvider = new POSProvider(cashboxId, accesstoken, isSandbox, logLevel, scuParams);
            Binder = new POSProviderBinder(this);

            _stopBroadcastReceiver = new StopBroadcastReceiver();

            _stopBroadcastReceiver.StopLauncherReceived += async () => {
                try
                {
                    await StopAsync();

                    Stop(ServiceConnectionProvider.GetConnection());
                }
                finally
                {
                    StopSelf();

                    Android.Widget.Toast.MakeText(Application.Context, $"Stopped {Application.Context.PackageName}", Android.Widget.ToastLength.Short).Show();

                    var intent = new Intent(Constants.BroadcastConstants.StopLauncherBroadcastName);
                    intent.SetPackage(Application.Context.PackageName);
                    SendBroadcast(intent);
                }
            };

            RegisterReceiver(_stopBroadcastReceiver, new IntentFilter(Constants.BroadcastConstants.StopBroadcastName));

            return Binder;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            var notification = GetNotification(LauncherState.NotConnected, false);

            StartForeground(NOTIFICATION_ID, notification);

            return base.OnStartCommand(intent, flags, startId);
        }

        public override void OnDestroy()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
                StopForeground(true);
            }

            StopSelf();
            Task.Run(() => StopAsync()).Wait();
            base.OnDestroy();
        }

        public async Task<IPOS> GetPOSAsync() => await _posProvider.GetPOSAsync();

        public async Task StopAsync() => await _posProvider.StopAsync();

        public static void Start(IMiddlewareServiceConnection serviceConnection, string cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> additionalScuParams, bool enableCloseButton)
        {
            if (!IsRunning(typeof(MiddlewareLauncherService)))
            {
                var intent = new Intent(Application.Context, typeof(MiddlewareLauncherService));
                intent.PutExtra("cashboxid", cashboxId);
                intent.PutExtra("accesstoken", accessToken);
                intent.PutExtra("sandbox", isSandbox);
                intent.PutExtra("loglevel", logLevel.ToString());
                intent.PutExtra("enableCloseButton", enableCloseButton);
                intent.PutExtras(additionalScuParams);

                Application.Context.BindService(intent, serviceConnection, Bind.AutoCreate);
                Application.Context.StartForegroundServiceCompat<MiddlewareLauncherService>();
            }
        }

        public static void Stop(IMiddlewareServiceConnection serviceConnection)
        {
            // TODO: helipad-upload

            if (IsRunning(typeof(MiddlewareLauncherService)))
            {
                serviceConnection.OnManualDisconnect();
                Application.Context.UnbindService(serviceConnection);
                var intent = new Intent(Application.Context, typeof(MiddlewareLauncherService));
                Application.Context.StopService(intent);
            }
        }

        public static void SetState(LauncherState state, bool enableCloseButton = false, string contentText = null)
        {
            var notification = GetNotification(state, enableCloseButton, contentText);
            var manager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
            manager.Notify(NOTIFICATION_ID, notification);
        }

        private static Notification GetNotification(LauncherState state, bool enableCloseButton, string contentText = null)
        {
            var icon = state switch
            {
                LauncherState.NotConnected => Resource.Drawable.ft_notification_notconnected,
                LauncherState.Connected => Resource.Drawable.ft_notification_connected,
                LauncherState.Error => Resource.Drawable.ft_notification_error,
                _ => throw new NotImplementedException(),
            };
            var text = state switch
            {
                LauncherState.NotConnected => "The fiskaltrust Middleware is on standby. Starting will take a few seconds, depending on the TSE.",
                LauncherState.Connected => "The fiskaltrust Middleware is running.",
                LauncherState.Error => "An error occured in the fiskaltrust Middleware. Please restart it.",
                _ => throw new NotImplementedException(),
            };
            if (contentText != null)
                text = contentText;

            var builder = new NotificationCompat.Builder(Application.Context, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle(Application.Context.Resources.GetString(Resource.String.app_name))
                .SetContentText(text)
                .SetCategory(Notification.CategoryService)
                .SetSmallIcon(icon)
                .SetOngoing(true);

            if(enableCloseButton)
            {
                Intent intent = new Intent(Constants.BroadcastConstants.StopBroadcastName);
                intent.SetPackage(Application.Context.PackageName);

                PendingIntent pendingIntent = PendingIntent.GetBroadcast(Application.Context, 0, intent, 0);

                builder.AddAction(Android.Resource.Drawable.IcMenuCloseClearCancel, "Stop Service", pendingIntent);
            }

            return builder.Build();
        }

        private static void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "fiskaltrust Middleware", NotificationImportance.Default)
                {
                    Description = "The fiskaltrust Middleware"
                };
                var manager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
                manager.CreateNotificationChannel(channel);
            }
        }

        private static bool IsRunning(Type type)
        {
            var manager = (ActivityManager)Application.Context.GetSystemService(ActivityService);

            foreach (var service in manager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(type).CanonicalName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}