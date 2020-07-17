using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.ifPOS.v1;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher
{
    [Service(Name = "eu.fiskaltrust.MiddlewareLauncherService")]
    public class MiddlewareLauncherService : Service, IPOSProvider
    {
        private const int SERVICE_RUNNING_NOTIFICATION_ID = 0x66746d77;
        private const string NOTIFICATION_CHANNEL_ID = "eu.fiskaltrust.launcher.android";

        static readonly string TAG = typeof(MiddlewareLauncherService).FullName;
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

        public static void Start(IServiceConnection serviceConnection, string cashboxId, string accessToken)
        {
            Intent intent = new Intent(Application.Context, typeof(MiddlewareLauncherService));
            intent.PutExtra("cashboxid", cashboxId);
            intent.PutExtra("accesstoken", accessToken);
            Application.Context.BindService(intent, serviceConnection, Bind.AutoCreate);
            Application.Context.StartForegroundServiceCompat<MiddlewareLauncherService>();
        }

        public static void Stop()
        {
            // TODO: helipad-upload

            var intent = new Intent(Application.Context, typeof(MiddlewareLauncherService));
            Application.Context.StopService(intent);
        }
    }
}