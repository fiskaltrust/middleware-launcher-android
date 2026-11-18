using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Enums;
using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.AndroidLauncher.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Models;
using fiskaltrust.AndroidLauncher.Notifications;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Services.Helper;
using fiskaltrust.AndroidLauncher.Services.InStoreApp;
using fiskaltrust.AndroidLauncher.Services.POSSystemApiCore;
using fiskaltrust.Api.PosSystem.Core;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Local;
using fiskaltrust.CloudCashbox.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Services;

internal class PosSystemAPIProvider {
    private readonly IConfigurationProvider _configurationProvider;
    private readonly ILauncherStateNotifier _stateNotifier;

    private SemaphoreSlim _startupLock = new(1);
    private PosSystemApiCore? _posSystemApiCore = null;
    private IMiddlewareProvider? _middlewareProvider = null;
    private IHost? _host = null;
    private IHelipadUploader? _helipadUploader = null;
    private bool _running = false;
    private int _inFlight = 0;

    private const string MessageBusSandboxUri = "gateway-sandbox.fiskaltrust.eu/mqtt";
    private const string MessageBusProductionUri = "gateway.fiskaltrust.eu/mqtt";

    public PosSystemAPIProvider(IConfigurationProvider configurationProvider, ILauncherStateNotifier stateNotifier)
    {
        _configurationProvider = configurationProvider;
        _stateNotifier = stateNotifier;
    }

    /// <summary>
    /// Handles the request with the running core, starting it if needed. The request counts as in flight for its
    /// whole duration, so <see cref="Restart"/> cannot dispose the host (and with it the in-store app response
    /// handlers) underneath it.
    /// </summary>
    public async Task<PosSystemApiResponse> HandleAsync(PosSystemApiRequest request, Action<string>? progressReporter = null)
    {
        // Incremented before Get so that a restart holding the startup lock sees requests that are still queued.
        Interlocked.Increment(ref _inFlight);
        try
        {
            var core = await Get(request.CashBoxId!.Value, request.AccessToken, progressReporter).ConfigureAwait(false);
            progressReporter?.Invoke(ActivityStages.STAGE_PROCESSING_REQUEST);
            return await core.HandleAsync(request).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    private async Task<PosSystemApiCore> Get(Guid cashboxId, string accessToken, Action<string>? progressReporter = null)
    {
        try
        {
            await _startupLock.WaitAsync();
            if (_posSystemApiCore is null)
            {
                await Start(cashboxId, accessToken, progressReporter);
            }

            if (_middlewareProvider is null || _middlewareProvider.CashboxId != cashboxId || _middlewareProvider.AccessToken != accessToken)
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
        var (configuration, configurationJson, isSandbox) = await _configurationProvider.GetCashboxConfigurationAsync(cashboxId, accessToken);

        progressReporter?.Invoke(ActivityStages.STAGE_STARTING_MIDDLEWARE);
        var middlewareTelemetryInitializer = new MiddlewareTelemetryInitializer("fiskaltrust.AndroidLauncher", VersionTracking.CurrentVersion, cashboxId);

        var telemetryConfiguration = new TelemetryConfiguration(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox));
        telemetryConfiguration.TelemetryInitializers.Add(middlewareTelemetryInitializer);

        var logLevel = Microsoft.Extensions.Logging.LogLevel.Information;
        LauncherLogging.InitializeWithTelemetry(telemetryConfiguration, logLevel);

        Log.Logger.Information("Starting the fiskaltrust.Middleware...");

        Log.Logger.Debug($"CashBox ID: {cashboxId}, IsSandbox: {isSandbox}");


        try
        {
            var loggerFactory = IPlatformApplication.Current?.Services.GetService<ILoggerFactory>() ?? LoggerFactory.Create(_ => { });
            var countryCode = Helpers.Configuration.GetQueueLocalization(configuration.ftQueues.First());
            var isV2Queue = countryCode == "PL";

            _middlewareProvider = isV2Queue
                ? new MiddlewareV2Provider(cashboxId, accessToken, configuration, countryCode, isSandbox, loggerFactory)
                : new MiddlewareProvider(cashboxId, accessToken, configuration, isSandbox, logLevel);
            await _middlewareProvider.StartAsync();

            progressReporter?.Invoke(ActivityStages.STAGE_STARTING_CORE);

            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            var services = builder.Services;
            services.AddLogging();
            services.AddSingleton(loggerFactory);

            services.AddOptions<PosSystemApiCoreOptions>().Configure(o =>
            {
                o.Sandbox = isSandbox;
                o.LauncherEnvironment = LauncherEnvironments.Local;
                o.MessageBusUri = isSandbox ? MessageBusSandboxUri : MessageBusProductionUri;
            });

            services.AddPosSystemApiCoreForLocal();
            services.AddSingleton<IMiddlewareConfigurationService>(
                new AndroidMiddlewareConfigurationService(cashboxId, accessToken, configurationJson));

            var middlewareClient = _middlewareProvider.MiddlewareClient;
            services.AddSingleton(middlewareClient);
            services.AddSingleton<IInStoreAppService, InStoreAppServiceRouter>();
            if (isV2Queue)
            {
                services.AddSingleton<Helper.IHelipadService>(serviceProvider =>
                    new Helper.HelipadService(
                        serviceProvider.GetRequiredService<ILogger<Helper.HelipadService>>(),
                        isSandbox));
                services.AddSingleton<IHelipadUploader>(serviceProvider =>
                    new Helper.HelipadUploader(
                        serviceProvider.GetRequiredService<Helper.IHelipadService>(),
                        serviceProvider.GetRequiredService<ILogger<Helper.HelipadUploader>>(),
                        new CashBoxCredentials
                        {
                            CashBoxId = cashboxId,
                            AccessToken = accessToken,
                            QueueId = configuration.ftQueues.Single().Id
                        },
                        configuration,
                        serviceProvider.GetRequiredService<PosSystemApiCore>()));
                services.AddHostedService<HelipadUploaderHostedService>();
            }
            //add Sqlite Storage services and dependencies
            var databasePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                $"possystemapi-{cashboxId}-{configuration.ftQueues.Single().Id}.sqlite");
            var migrationsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "POSMigrations");
            CopyPosMigrationsToDataDir(migrationsPath);
            services.AddSingleton<IStorageFactory, StorageFactory>( _ => new StorageFactory(loggerFactory, databasePath));

            _host = builder.Build();
            // Starts the hosted services registered by the core (e.g. the MQTT client).
            await _host.StartAsync().ConfigureAwait(false);

            _posSystemApiCore = _host.Services.GetRequiredService<PosSystemApiCore>();            
           
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

    public static void CopyPosMigrationsToDataDir(string targetDirectory)
    {
        const string migrationDir = "POSMigrations";

        var assets = Android.App.Application.Context.Assets.List(migrationDir);
        Directory.CreateDirectory(targetDirectory);
        foreach (var asset in assets)
        {
            var targetFile = System.IO.Path.Combine(targetDirectory, asset);
            if (!File.Exists(targetFile))
            {
                using var source = Android.App.Application.Context.Assets.Open(System.IO.Path.Combine(migrationDir, asset));
                using var target = File.Create(targetFile);
                source.CopyTo(target);
            }
        }
    }

    private async Task Stop(bool forceStop = false) {

        try
        {
            if (_host is not null)
            {
                try
                {
                    await _host.StopAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning(ex, "Failed to stop the PosSystemApi host.");
                }
            }

            if ((_running || forceStop) && _middlewareProvider is not null)
            {
                await _middlewareProvider.StopAsync();
            }
        }
        finally
        {
            _host?.Dispose();
            _host = null;
            _middlewareProvider = null;
            _posSystemApiCore = null;
            _helipadUploader = null;
            _running = false;
        }
    }


    /// <returns>False if the restart was refused because requests are still in progress.</returns>
    public async Task<bool> Restart(Guid cashboxId, string accessToken, Action<string>? progressReporter = null)
    {
        try
        {
            await _startupLock.WaitAsync();
            if (Volatile.Read(ref _inFlight) > 0)
            {
                Log.Logger.Warning("Refusing to restart the PosSystemApi while requests are in progress.");
                return false;
            }

            await Stop();
            await Start(cashboxId, accessToken, progressReporter);
            return true;
        }
        finally
        {
            _startupLock.Release();
        }
    }
}
