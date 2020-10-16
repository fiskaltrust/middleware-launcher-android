using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using fiskaltrust.AndroidLauncher.Enums;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers.Hosting;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.ifPOS.v1;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.AndroidService
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherService")]
    public class MiddlewareLauncherService : Service, IPOSProvider
    {
        private const int NOTIFICATION_ID = 0x66746d77;
        private const string NOTIFICATION_CHANNEL_ID = "eu.fiskaltrust.launcher.android";

        private IPOSProvider _posProvider;

        public IBinder Binder { get; private set; }

        public override IBinder OnBind(Intent intent)
        {
            var cashboxIdString = intent.GetStringExtra("cashboxid");
            var accesstoken = intent.GetStringExtra("accesstoken");
            var isSandbox = intent.GetBooleanExtra("sandbox", false);
            var scuParams = intent.GetScuConfigParameters(removePrefix: true);            

            if (string.IsNullOrEmpty(cashboxIdString) || !Guid.TryParse(cashboxIdString, out var cashboxId))
            {
                throw new ArgumentException("The extra 'cashboxid' needs to be set in this intent.", "cashboxid");
            }
            if(string.IsNullOrEmpty(accesstoken))
            {
                throw new ArgumentException("The extra 'accesstoken' needs to be set in this intent.", "accesstoken");
            }

            _posProvider = new POSProvider(cashboxId, accesstoken, isSandbox, scuParams);
            Binder = new POSProviderBinder(this);
            return Binder;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();           
            var notification = GetNotification(LauncherState.NotConnected);

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

        public static void Start(IMiddlewareServiceConnection serviceConnection, string cashboxId, string accessToken, bool isSandbox, Dictionary<string, object> additionalScuParams)
        {
            if (!IsRunning(typeof(MiddlewareLauncherService)))
            {
                var intent = new Intent(Application.Context, typeof(MiddlewareLauncherService));
                intent.PutExtra("cashboxid", cashboxId);
                intent.PutExtra("accesstoken", accessToken);
                intent.PutExtra("sandbox", isSandbox);
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

        public static void SetState(LauncherState state, string contentText = null)
        {
            var notification = GetNotification(state, contentText);
            var manager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
            manager.Notify(NOTIFICATION_ID, notification);
        }

        private static Notification GetNotification(LauncherState state, string contentText = null)
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

            var builder = new Notification.Builder(Application.Context, NOTIFICATION_CHANNEL_ID)
              .SetContentTitle(Application.Context.Resources.GetString(Resource.String.app_name))
              .SetContentText(text)
              .SetCategory(Notification.CategoryService)
              .SetSmallIcon(icon)
              .SetOngoing(true);
            return builder.Build();
        }

        private static void CreateNotificationChannel()
        {
            var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "fiskaltrust Middleware", NotificationImportance.Default)
            {
                Description = "The fiskaltrust Middleware"
            };
            var manager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
            manager.CreateNotificationChannel(channel);
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