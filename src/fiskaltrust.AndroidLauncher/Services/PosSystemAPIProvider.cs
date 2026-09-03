using Android.App;
using Android.OS;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Enums;
using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.AndroidLauncher.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Notifications;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Services.POSSystemApiCore;
using fiskaltrust.AndroidLauncher.Storage;
using fiskaltrust.Api.PosSystem.Core;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using Java.Util;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Services;

internal class PosSystemAPIProvider {
    private readonly IConfigurationProvider _configurationProvider;
    private readonly ILauncherStateNotifier _stateNotifier;

    private SemaphoreSlim _startupLock = new(1);
    private PosSystemApiCore? _posSystemApiCore = null;
    private MiddlewareProvider? _middlewareProvider = null;
    private bool _running = false;

    public PosSystemAPIProvider(IConfigurationProvider configurationProvider, ILauncherStateNotifier stateNotifier)
    {
        _configurationProvider = configurationProvider;
        _stateNotifier = stateNotifier;
    }

    public async Task<PosSystemApiCore> Get(Guid cashboxId, string accessToken, Action<string>? progressReporter = null)
    {
        try
        {
            await _startupLock.WaitAsync();
            if (_posSystemApiCore is null)
            {
                await Start(cashboxId, accessToken, progressReporter);
            }

            if (_middlewareProvider.CashboxId != cashboxId || _middlewareProvider.AccessToken != accessToken)
            {
                throw new InvalidOperationException("The requested cashboxid or accesstoken do not match the currently running cashbox. To start the requested cashbox send an echo null.");
            }

            return _posSystemApiCore;
        }
        finally
        {
            _startupLock.Release();
        }
    }

    private async Task Start(Guid cashboxId, string accessToken, Action<string>? progressReporter = null) {

        progressReporter?.Invoke(ActivityStages.STAGE_GETTING_CONFIGURATION);
        var (configuration, isSandbox) = await _configurationProvider.GetCashboxConfigurationAsync(cashboxId, accessToken);

        progressReporter?.Invoke(ActivityStages.STAGE_STARTING_MIDDLEWARE);
        var middlewareTelemetryInitializer = new MiddlewareTelemetryInitializer("fiskaltrust.AndroidLauncher", VersionTracking.CurrentVersion, cashboxId);

        var telemetryConfiguration = new TelemetryConfiguration(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox));
        telemetryConfiguration.TelemetryInitializers.Add(middlewareTelemetryInitializer);

        var logLevel = Microsoft.Extensions.Logging.LogLevel.Information;
        LauncherLogging.InitializeWithTelemetry(telemetryConfiguration, logLevel);

        Log.Logger.Information("Starting the fiskaltrust.Middleware...");


        Log.Logger.Debug($"CashBox ID: {cashboxId}, IsSandbox: {isSandbox}");
        _middlewareProvider = new MiddlewareProvider(cashboxId, accessToken, configuration, isSandbox, logLevel);
        try
        {
            await _middlewareProvider.StartAsync();

            var config = new POSSystemApiCoreConfiguration
            {
                CashBoxId = cashboxId,
                AccessToken = accessToken,
                Configuration = Newtonsoft.Json.JsonConvert.SerializeObject(configuration),
                AppEnvironment = isSandbox ? AppEnvironments.Sandbox : AppEnvironments.Production,
                LauncherEnvironment = LauncherEnvironments.Local
            };

            progressReporter?.Invoke(ActivityStages.STAGE_STARTING_CORE);
            var bootstrapper = new POSCoreBootstrapper();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            bootstrapper.Configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            var services = new ServiceCollection();
            await bootstrapper.ConfigureServices(services);

            services.AddSingleton(_middlewareProvider.MiddlewareClientAndroid);
            services.AddSingleton<IOperationItemRepository, MemoryOperationItemRepository>();
            services.AddSingleton<IStorageFactory, StorageFactory>();

            var provider = services.BuildServiceProvider();
            _posSystemApiCore = provider.GetRequiredService<PosSystemApiCore>();

            _stateNotifier.Notify(LauncherState.Connected);
            _running = true;
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                ex = ex.InnerException;

            Log.Logger.Error(ex, "An error occured while trying to start the fiskaltrust Android Launcher.");
            if (ex is RemountRequiredException remountRequiredEx)
            {
                _stateNotifier.Notify(LauncherState.Error, remountRequiredEx.Message);
            }
            else if (ex is ConfigurationNotFoundException confNotFoundEx)
            {
                _stateNotifier.Notify(LauncherState.Error, confNotFoundEx.Message);
            }
            else
            {
                _stateNotifier.Notify(LauncherState.Error);
            }

            await Stop(true);
            throw;
        }
    }

    private async Task Stop(bool forceStop = false) {

        try
        {
            if((_running || forceStop) && _middlewareProvider is not null)
            {
                await _middlewareProvider.StopAsync();
            }
        }
        finally
        {
            _middlewareProvider = null;
            _posSystemApiCore = null;
            _running = false;
        }
    }

    public async Task Restart(Guid cashboxId, string accessToken, Action<string>? progressReporter = null)
    {
        try
        {
            await _startupLock.WaitAsync();
            await Stop();
            await Start(cashboxId, accessToken, progressReporter);
        }
        finally
        {
            _startupLock.Release();
        }
    }
}
