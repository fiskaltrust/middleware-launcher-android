using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.AndroidService
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
            CreateNotificationChannel();
            var notification = GetNotification(LauncherState.NotConnected);
            StartForeground(NOTIFICATION_ID, notification);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(path: Path.Combine(FileLoggerHelper.LogDirectory.FullName, FileLoggerHelper.LogFilename), rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31)
                .CreateLogger();

            try
            {
                Log.Logger.Information("Starting the fiskaltrust.Middleware...");

                var cashboxIdString = intent.GetStringExtra("cashboxid");
                var accessToken = intent.GetStringExtra("accesstoken");
                var isSandbox = intent.GetBooleanExtra("sandbox", false);
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
                        Log.Logger.Error(ex, "AdminEndpointService starting failed...");
                    }

                    await _launcher.StartAsync();

                    SetState(LauncherState.Connected);
                    StateProvider.Instance.SetState(State.Running);
                }).Wait();

                return StartCommandResult.RedeliverIntent;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "An error occured while trying to start the fiskaltrust Android Launcher.");

                if (ex is RemountRequiredException remountRequiredEx)
                {
                    StateProvider.Instance.SetState(State.Error, StateReasons.RemountRequired);
                    SetState(LauncherState.Error, remountRequiredEx.Message);
                }
                else if (ex is ConfigurationNotFoundException confNotFoundEx)
                {
                    StateProvider.Instance.SetState(State.Error, StateReasons.ConfigurationNotFound);
                    SetState(LauncherState.Error, confNotFoundEx.Message);
                }
                else
                {
                    StateProvider.Instance.SetState(State.Error, ex.Message);
                    SetState(LauncherState.Error);
                }

                throw;
            }
        }

        public override void OnDestroy()
        {
            Log.Logger.Information("Destroying the fiskaltrust.Middleware service");
            
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

            base.OnDestroy();
        }

        public static void Start<T>(string cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> additionalScuParams) where T : MiddlewareLauncherService
        {
            if (!IsRunning(typeof(T)))
            {
                var bundle = new Bundle();
                bundle.PutString("cashboxid", cashboxId);
                bundle.PutString("accesstoken", accessToken);
                bundle.PutBoolean("sandbox", isSandbox);
                bundle.PutString("loglevel", logLevel.ToString());
                foreach (var extra in additionalScuParams)
                {
                    bundle.PutString(extra.Key, extra.Value.ToString());
                }

                Application.Context.StartForegroundServiceCompat<T>(bundle);
            }
        }

        public static void Stop<T>() where T : MiddlewareLauncherService
        {
            // TODO: helipad-upload
            Log.Logger.Information("Stopping the fiskaltrust.Middleware service");

            if (IsRunning(typeof(T)))
            {
                var intent = new Intent(Application.Context, typeof(T));
                Application.Context.StopService(intent);
            }
        }

        public static void SetState(LauncherState state, string contentText = null)
        {
            var notification = GetNotification(state, contentText);
            var manager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
            manager.Notify(NOTIFICATION_ID, notification);
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
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

            var builder = new NotificationCompat.Builder(Application.Context, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle(Application.Context.Resources.GetString(Resource.String.app_name))
                .SetContentText(text)
                .SetCategory(Notification.CategoryService)
                .SetSmallIcon(icon)
                .SetOngoing(true);

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