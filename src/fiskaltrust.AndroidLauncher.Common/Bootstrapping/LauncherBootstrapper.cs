using Android.Content;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Models;
using fiskaltrust.AndroidLauncher.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Bootstrapping
{
    public static class LauncherBootstrapper
    {
        public static void Setup(Context context, Intent startIntent)
        {
            var cashboxId = startIntent.GetStringExtra("cashboxid");
            var accessToken = startIntent.GetStringExtra("accesstoken");
            var isSandbox = startIntent.GetBooleanExtra("sandbox", false);
            var logLevel = Enum.TryParse(startIntent.GetStringExtra("loglevel"), out LogLevel level) ? level : LogLevel.Information;
            var scuParams = startIntent.GetScuConfigParameters();

            MiddlewareLauncherService.Start(ServiceConnectionProvider.GetConnection(), new LauncherParameters { CashboxId = new Guid(cashboxId), AccessToken = accessToken, IsSandbox = isSandbox, LogLevel = logLevel, ScuParams = scuParams });

            using var services = new ServiceCollection().AddLogProviders(logLevel).BuildServiceProvider();
            var logger = services.GetRequiredService<ILogger<MiddlewareLauncherService>>();

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
            });
        }

        public static void Teardown(Context context)
        {
            MiddlewareLauncherService.Stop(ServiceConnectionProvider.GetConnection());
            StateProvider.Instance.SetState(State.Uninitialized);
        }
    }
}