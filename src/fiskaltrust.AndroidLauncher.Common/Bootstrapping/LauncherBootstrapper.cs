using Android.Content;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Common.Bootstrapping
{
    public static class LauncherBootstrapper
    {
        private static readonly Semaphore _semaphore = new Semaphore(1, 1);

        public static void Setup(Context context, Intent startIntent)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(path: Path.Combine(FileLoggerHelper.LogDirectory.FullName, FileLoggerHelper.LogFilename), rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31)
                .CreateLogger();
            
            var cashboxId = startIntent.GetStringExtra("cashboxid");
            var accessToken = startIntent.GetStringExtra("accesstoken");
            var isSandbox = startIntent.GetBooleanExtra("sandbox", false);
            var logLevel = Enum.TryParse(startIntent.GetStringExtra("loglevel"), out LogLevel level) ? level : LogLevel.Information;
            var scuParams = startIntent.GetScuConfigParameters();
            
            using var services = new ServiceCollection().AddLogProviders(logLevel).BuildServiceProvider();
            var logger = services.GetRequiredService<ILogger<MiddlewareLauncherService>>();

            if (!_semaphore.WaitOne(0))
            {
                logger.LogWarning("Received start intent, but the Middleware is already starting.");
                return;
            }

            logger.LogInformation("Starting the Middleware..");
            MiddlewareLauncherService.Start(ServiceConnectionProvider.GetConnection(), cashboxId, accessToken, isSandbox, logLevel, scuParams);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000);

                    // Call once to initialize Middleware. Will be replaced with a cleaner approach soon.
                    var pos = await ServiceConnectionProvider.GetConnection().GetPOSAsync();
                    if (pos == null)
                    {
                        logger.LogWarning("GetPOSAsync returned null while starting the Launcher.");
                    }

                    MiddlewareLauncherService.SetState(LauncherState.Connected);
                    StateProvider.Instance.SetState(State.Running);
                    logger.LogInformation("Successfully started the Middleware.");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "An error occured while trying to start the fiskaltrust Android Launcher.");

                    if (ex is RemountRequiredException remountRequiredEx)
                    {
                        StateProvider.Instance.SetState(State.Error, StateReasons.RemountRequired);
                        MiddlewareLauncherService.SetState(LauncherState.Error, remountRequiredEx.Message);
                    }
                    else if (ex is ConfigurationNotFoundException confNotFoundEx)
                    {
                        StateProvider.Instance.SetState(State.Error, StateReasons.ConfigurationNotFound);
                        MiddlewareLauncherService.SetState(LauncherState.Error, confNotFoundEx.Message);
                    }
                    else
                    {
                        StateProvider.Instance.SetState(State.Error, ex.Message);
                        MiddlewareLauncherService.SetState(LauncherState.Error);
                    }
                }

                _semaphore.Release();
            });
        }

        public static void Teardown(Context context)
        {
            MiddlewareLauncherService.Stop(ServiceConnectionProvider.GetConnection());
            StateProvider.Instance.SetState(State.Uninitialized);
        }
    }
}