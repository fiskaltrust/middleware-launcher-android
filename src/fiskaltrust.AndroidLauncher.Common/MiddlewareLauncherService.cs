using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.ifPOS.v1;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherService")]
    public class MiddlewareLauncherService : Service, IPOSProvider
    {
        private const int SERVICE_RUNNING_NOTIFICATION_ID = 0x66746d77;
        private const string NOTIFICATION_CHANNEL_ID = "eu.fiskaltrust.launcher.android";

        private IPOSProvider _posProvider;

        public IBinder Binder { get; private set; }

        public override IBinder OnBind(Intent intent)
        {
            var cashboxIdString = intent.GetStringExtra("cashboxid");
            var accesstoken = intent.GetStringExtra("accesstoken");

            if(string.IsNullOrEmpty(cashboxIdString) || !Guid.TryParse(cashboxIdString, out var cashboxId))
            {
                throw new ArgumentException("The extra 'cashboxid' needs to be set in this intent.", "cashboxid");
            }
            if(string.IsNullOrEmpty(accesstoken))
            {
                throw new ArgumentException("The extra 'accesstoken' needs to be set in this intent.", "accesstoken");
            }

            _posProvider = new POSProvider(cashboxId, accesstoken);
            Binder = new POSProviderBinder(this);
            return Binder;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            var channelId = CreateNotificationChannel();
            var builder = new Notification.Builder(this, channelId)
               .SetContentTitle(Resources.GetString(Resource.String.app_name))
               .SetContentText("The fiskaltrust Middleware.Launcher is running.")
               .SetCategory(Notification.CategoryService)
               .SetSmallIcon(Resource.Drawable.ft_notification)
               .SetOngoing(true);

            StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, builder.Build());

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
            StopAsync().Wait();
            base.OnDestroy();
        }

        private string CreateNotificationChannel()
        {
            NotificationChannel chan = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "fiskaltrust Middleware", NotificationImportance.Default)
            {
                Description = "The fiskaltrust Middleware"
            };
            NotificationManager manager = (NotificationManager)this.GetSystemService(NotificationService);
            manager.CreateNotificationChannel(chan);

            return NOTIFICATION_CHANNEL_ID;
        }

        public Task<IPOS> GetPOSAsync() => _posProvider.GetPOSAsync();

        public Task StopPOSAsync() => _posProvider.StopAsync();

        public static void Start(IMiddlewareServiceConnection serviceConnection, string cashboxId, string accessToken)
        {
            if (!IsRunning(typeof(MiddlewareLauncherService)))
            {
                var intent = new Intent(Application.Context, typeof(MiddlewareLauncherService));
                intent.PutExtra("cashboxid", cashboxId);
                intent.PutExtra("accesstoken", accessToken);
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

        public Task StopAsync() => _posProvider.StopAsync();

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