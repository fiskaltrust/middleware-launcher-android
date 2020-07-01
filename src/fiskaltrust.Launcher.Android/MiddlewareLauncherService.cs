using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.ifPOS.v1;
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
        public override void OnCreate()
        {
            base.OnCreate();
            Log.Debug(TAG, "OnCreate");
            _posProvider = new POSProvider();
        }

        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(TAG, "OnBind");
            Binder = new POSProviderBinder(this);
            return Binder;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            var channelId = CreateNotificationChannel();
            var notification = new Notification.Builder(this, channelId)
               .SetContentTitle(Resources.GetString(Resource.String.app_name))
               .SetContentText("The fiskaltrust Middleware.Launcher is running in the background.")
               //.SetSmallIcon(Resource.Drawable.ic_stat_name)
               //.SetContentIntent(BuildIntentToShowMainActivity())
               .SetOngoing(true)
               //.AddAction(BuildRestartTimerAction())
               //.AddAction(BuildStopServiceAction())
               .Build();

            // Enlist this instance of the service as a foreground service
            StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);

            return base.OnStartCommand(intent, flags, startId);
        }

        private string CreateNotificationChannel()
        {
            NotificationChannel chan = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "fiskaltrust Middleware", NotificationImportance.High);
            NotificationManager manager = (NotificationManager)this.GetSystemService(NotificationService);
            manager.CreateNotificationChannel(chan);

            return NOTIFICATION_CHANNEL_ID;
        }

        public Task<IPOS> GetPOSAsync() => _posProvider.GetPOSAsync();
    }
}