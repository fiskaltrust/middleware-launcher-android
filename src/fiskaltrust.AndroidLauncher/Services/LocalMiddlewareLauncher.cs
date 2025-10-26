using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Services.Helper;
using fiskaltrust.AndroidLauncher.Services.Queue;
using fiskaltrust.AndroidLauncher.Services.SCU;
using fiskaltrust.AndroidLauncher.Signing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class LocalMiddlewareLauncher : IMiddlewareLauncher
    {
        private const string PACKAGE_NAME_DE_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_DE_FISKALY_CERTIFIED = "fiskaltrust.Middleware.SCU.DE.FiskalyCertified";
        private const string PACKAGE_NAME_IT_EPSON_RT_PRINTER = "fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter";
        private const string PACKAGE_NAME_IT_CUSTOM_RT_SERVER = "fiskaltrust.Middleware.SCU.IT.CustomRTServer";

        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILocalConfigurationProvider _localConfigurationProvider;

        private readonly Guid _cashboxId;
        private readonly string _accessToken;
        private readonly bool _isSandbox;
        private readonly Dictionary<string, object> _scuParams;
        private readonly LogLevel _logLevel;

        private Android.OS.PowerManager.WakeLock _wakeLock;
        private List<IHelper> _helpers;
        private IPOS _poss;
        private AbstractScuList _scus;

        public bool IsRunning { get; set; }

        public IPOS POS => _poss;

        public LocalMiddlewareLauncher(Guid cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> scuParams)
        {
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;
            _scuParams = scuParams;
            _logLevel = logLevel;

            _configurationProvider = new HelipadConfigurationProvider();
            _localConfigurationProvider = new LocalConfigurationProvider();
            _helpers = new List<IHelper>();
            _scus = new AbstractScuList();
        }

        public async Task StartAsync()
        {
            ftCashBoxConfiguration configuration;
            try
            {
                configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken, _isSandbox);
                await _localConfigurationProvider.PersistAsync(_cashboxId, _accessToken, configuration);
            }
            catch (Exception e)
            {
                try
                {
                    configuration = await _localConfigurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken, _isSandbox);
                }
                catch
                {
                    throw new ConfigurationNotFoundException($"The configuration for the cashbox {_cashboxId} could not be downloaded. An internet connection is required at least on the initialization attempt of a cashbox.", e);
                }
            }

            foreach (var scuConfig in configuration.ftSignaturCreationDevices)
            {
                scuConfig.Configuration["sandbox"] = _isSandbox;

                switch (scuConfig.Package)
                {
                    case PACKAGE_NAME_DE_SWISSBIT:
                        // On some (payment) devices, the CPU is turned off as soon as the device becomes remotely idle (i.e. right after processing a receipt) - this seems to also stop the internal clock of the Swissbit TSE.
                        // To prevent this, we acquire a partial wake lock to keep the CPU running. As this is only required with hardware TSEs, we only acquire the wake lock for the Swissbit SCU for now.
                        AcquireCpuWakeLock();
                        await InitializeDESwissbitScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_DE_FISKALY_CERTIFIED:
                        await InitializeDEFiskalyCertifiedScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_IT_EPSON_RT_PRINTER:
                        await InitializeITEpsonRTPrinterSCUAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_IT_CUSTOM_RT_SERVER:
                        await InitializeITCustomRTServerScuAsync(scuConfig);
                        break;
                    default:
                        throw new ArgumentException($"The Android launcher currently only supports the following SCU packages: {PACKAGE_NAME_DE_SWISSBIT}, {PACKAGE_NAME_DE_FISKALY_CERTIFIED}, {PACKAGE_NAME_IT_EPSON_RT_PRINTER}.");
                }
            }

            if(configuration.ftQueues.Count() != 1)
            {
                throw new ArgumentException("The Android launcher currently only supports exactly one queue package.");
            }

            foreach (var queueConfig in configuration.ftQueues)
            {
                queueConfig.Configuration["sandbox"] = _isSandbox;
                await InitializeQueueAsync(queueConfig);
            }

            await InitializeHelipadHelperAsync(configuration);

            IsRunning = true;
        }

        public async Task StopAsync()
        {
            // LocalMiddlewareLauncher doesn't manage hosts directly, just helpers and wake locks
            foreach (var helper in _helpers)
            {
                helper.StopBegin();
                helper.StopEnd();
            }

            _wakeLock?.Release();

            IsRunning = false;
        }

        private async Task InitializeDESwissbitScuAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new DESwissbitScuProvider();
            var scu = scuProvider.CreateSCU(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created German SCU of type 'fiskaltrust.Middleware.SCU.DE.Swissbit'.");
        }

        private async Task InitializeDEFiskalyCertifiedScuAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new DEFiskalyCertifiedScuProvider();
            var scu = scuProvider.CreateSCU(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created German SCU of type 'fiskaltrust.Middleware.SCU.DE.FiskalyCertified'.");
        }

        private async Task InitializeITEpsonRTPrinterSCUAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new ITEpsonRTPrinterSCUProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created German SCU of type 'fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter'.");
        }

        private async Task InitializeITCustomRTServerScuAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new ITCustomRTServerScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created German SCU of type 'fiskaltrust.Middleware.SCU.IT.CustomRTServer'.");
        }

        private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        {
            var queueProvider = new SQLiteQueueProvider();
            var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _accessToken, _isSandbox, _logLevel, _scus));
            _poss = pos;
            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.Queue.SQLite' is listening on 'Intnet Interface'.");
        }

        private async Task InitializeHelipadHelperAsync(ftCashBoxConfiguration configuration)
        {
            var helipadHelperProvider = new HelipadHelperProvider();
            var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(configuration, _accessToken, _isSandbox, _logLevel, new List<IPOS> { _poss }));
            helper.StartBegin();
            helper.StartEnd();
            _helpers.Add(helper);
        }

        private static string GetPrimaryUriForSignaturCreationUnit(PackageConfiguration scuConfiguration)
        {
            var grpcUrl = scuConfiguration.Url.FirstOrDefault(x => x.StartsWith("grpc://", StringComparison.InvariantCulture));
            return new Uri(grpcUrl ?? scuConfiguration.Url.First()).ToString();
        }

        private void AcquireCpuWakeLock()
        {
            var pm = (Android.OS.PowerManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.PowerService);
            _wakeLock = pm.NewWakeLock(Android.OS.WakeLockFlags.Partial, "fiskaltrust.AndroidLauncher::KeepAliveWakeLock");
            _wakeLock.Acquire();
        }
    }
}