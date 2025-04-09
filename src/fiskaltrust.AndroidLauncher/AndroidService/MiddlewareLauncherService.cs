using Android.App;
using Android.Content;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Enums;
using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Hosting;
using fiskaltrust.AndroidLauncher.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.AndroidService
{
    public abstract class MiddlewareLauncherService : Service
    {
        private const int NOTIFICATION_ID = 0x66746d77;
        private const string NOTIFICATION_CHANNEL_ID = "eu.fiskaltrust.launcher.android";

        private MiddlewareLauncher _launcher;

        public abstract IHostFactory GetHostFactory();
        public abstract IUrlResolver GetUrlResolver();

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            OnBind(null);
            CreateNotificationChannel();

            var enableCloseButton = intent.GetBooleanExtra("enableCloseButton", false);
            var notification = GetNotification(LauncherState.NotConnected, enableCloseButton);

            if (Build.VERSION.SdkInt > BuildVersionCodes.Tiramisu)
            {
                // Android 14 requires us to specify the service type
                StartForeground(NOTIFICATION_ID, notification, Android.Content.PM.ForegroundService.TypeDataSync);
            }
            else
            {
                StartForeground(NOTIFICATION_ID, notification);
            }

            try
            {
                var isSandbox = intent.GetBooleanExtra("sandbox", false);
                var cashboxIdString = intent.GetStringExtra("cashboxid");
                var accessToken = intent.GetStringExtra("accesstoken");
                var logLevel = Enum.TryParse(intent.GetStringExtra("loglevel"), out LogLevel level) ? level : LogLevel.Information;
                var scuParams = intent.GetScuConfigParameters(removePrefix: true);

                if (string.IsNullOrEmpty(cashboxIdString) || !Guid.TryParse(cashboxIdString, out var cashboxId))
                {
                    throw new ArgumentException("The extra 'cashboxid' needs to be set in this intent.", "cashboxid");
                }
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new ArgumentException("The extra 'accesstoken' needs to be set in this intent.", "accesstoken");
                }



                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .CreateLogger();

                Log.Logger.Information("Starting the fiskaltrust.Middleware...");


                Log.Logger.Debug($"CashBox ID: {cashboxIdString}, IsSandbox: {isSandbox}");
                _launcher = new MiddlewareLauncher(GetHostFactory(), GetUrlResolver(), cashboxId, accessToken, isSandbox, logLevel, scuParams);
                Task.Run(async () =>
                {
                    try
                    {
                        await AdminEndpointService.Instance.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "AdminEndpointService starting failed.");
                    }

                    await _launcher.StartAsync();

                    SetState(LauncherState.Connected, enableCloseButton);
                    StateProvider.Instance.SetState(State.Running);
                }).Wait();

                Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

                return StartCommandResult.RedeliverIntent;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;

                Log.Logger.Error(ex, "An error occured while trying to start the fiskaltrust Android Launcher.");
                if (ex is RemountRequiredException remountRequiredEx)
                {
                    StateProvider.Instance.SetState(State.Error, StateReasons.RemountRequired);
                    SetState(LauncherState.Error, enableCloseButton, remountRequiredEx.Message);
                }
                else if (ex is ConfigurationNotFoundException confNotFoundEx)
                {
                    StateProvider.Instance.SetState(State.Error, StateReasons.ConfigurationNotFound);
                    SetState(LauncherState.Error, enableCloseButton, confNotFoundEx.Message);
                }
                else
                {
                    StateProvider.Instance.SetState(State.Error, ex.Message);
                    SetState(LauncherState.Error, enableCloseButton);
                }

                return StartCommandResult.NotSticky;
            }
        }

        public override void OnDestroy()
        {
            Log.Logger.Information("Destroying the fiskaltrust.Middleware service");
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;

            Task.Run(async () =>
            {
                await AdminEndpointService.Instance.StopAsync();
                await _launcher.StopAsync();
            }).Wait();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
                StopForeground(true);
            }

            Log.CloseAndFlush();
            base.OnDestroy();
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            Log.Logger.Warning($"Connectivity state has changed to {e.NetworkAccess}. Current connection profiles: {string.Join(", ", e.ConnectionProfiles.Select(x => x.ToString()))}");
        }

        public static void Start<T>(string cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> additionalScuParams, bool enableCloseButton) where T : MiddlewareLauncherService
        {
            if (!IsRunning(typeof(T)))
            {
                var bundle = new Bundle();
                bundle.PutString("cashboxid", cashboxId);
                bundle.PutString("accesstoken", accessToken);
                bundle.PutBoolean("sandbox", isSandbox);
                bundle.PutString("loglevel", logLevel.ToString());
                bundle.PutBoolean("enableCloseButton", enableCloseButton);
                foreach (var extra in additionalScuParams)
                {
                    bundle.PutString(extra.Key, extra.Value.ToString());
                }

                Android.App.Application.Context.StartForegroundServiceCompat<T>(bundle);
            }
        }

        public static void Stop<T>() where T : MiddlewareLauncherService
        {
            // TODO: helipad-upload
            Log.Logger.Information("Stopping the fiskaltrust.Middleware service");

            if (IsRunning(typeof(T)))
            {
                var intent = new Intent(Android.App.Application.Context, typeof(T));
                Android.App.Application.Context.StopService(intent);
            }
        }

        public static void SetState(LauncherState state, bool enableCloseButton, string contentText = null)
        {
            var notification = GetNotification(state, enableCloseButton, contentText);
            var manager = (NotificationManager)Android.App.Application.Context.GetSystemService(NotificationService);
            manager.Notify(NOTIFICATION_ID, notification);
        }

        private static Notification GetNotification(LauncherState state, bool enableCloseButton, string contentText = null)
        {
            // var icon = state switch
            // {
            //     LauncherState.NotConnected => Resource.Drawable.ft_notification_notconnected,
            //     LauncherState.Connected => Resource.Drawable.ft_notification_connected,
            //     LauncherState.Error => Resource.Drawable.ft_notification_error,
            //     _ => throw new NotImplementedException(),
            // };
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
                // .SetContentTitle(Android.App.Application.Context.Resources.GetString(Resource.String.app_name))
                .SetContentText(text)
                .SetCategory(Notification.CategoryService)
                // .SetSmallIcon(icon)
                .SetOngoing(true)
                .SetNotificationSilent();

            if (enableCloseButton)
            {
                Intent intent = new Intent(BroadcastConstants.StopBroadcastName);
                intent.SetPackage(Android.App.Application.Context.PackageName);

                PendingIntent pendingIntent = PendingIntent.GetBroadcast(Android.App.Application.Context, 0, intent, PendingIntentFlags.Immutable);

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
                var manager = (NotificationManager)Android.App.Application.Context.GetSystemService(NotificationService);
                manager.CreateNotificationChannel(channel);
            }
        }

        private static bool IsRunning(Type type)
        {
            var manager = (ActivityManager)Android.App.Application.Context.GetSystemService(ActivityService);

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