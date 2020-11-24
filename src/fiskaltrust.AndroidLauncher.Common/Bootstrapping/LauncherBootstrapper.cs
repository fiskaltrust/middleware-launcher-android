using Android.Content;
using Android.OS;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Hosting;
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

            Toast.MakeText(context, $"Starting fiskaltrust Middleware with cashbox '{cashboxId}' (Sandbox: {isSandbox}). Initializing might take up to 45 seconds, depending on the TSE.", ToastLength.Long).Show();

            Task.Run(async () => await AdminEndpointService.Instance.StartAsync()).Wait();
            MiddlewareLauncherService.Start(ServiceConnectionProvider.GetConnection(), cashboxId, accessToken, isSandbox, logLevel, scuParams);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000);

                    // Call once to initialize Middleware. Will be replaced with a cleaner approach soon.
                    await ServiceConnectionProvider.GetConnection().GetPOSAsync();

                    new Handler(Looper.MainLooper).Post(() => Toast.MakeText(context, $"Successfully started the fiskaltrust Middleware.", ToastLength.Long).Show());
                    MiddlewareLauncherService.SetState(LauncherState.Connected);
                    StateProvider.Instance.SetState(State.Running);
                }
                catch (System.Exception ex)
                {
                    using (var services = new ServiceCollection().AddLogProviders(logLevel).BuildServiceProvider())
                    {
                        var logger = services.GetRequiredService<ILogger<MiddlewareLauncherService>>();
                        logger.LogCritical(ex, "An error occured while trying to start the fiskaltrust Android Launcher.");
                    }

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
            Task.Run(async () => await AdminEndpointService.Instance.StopAsync()).Wait();
            StateProvider.Instance.SetState(State.Uninitialized);
            
            Toast.MakeText(context, $"fiskaltrust Middleware stopped.", ToastLength.Long).Show();
        }
    }
}