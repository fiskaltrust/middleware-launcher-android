using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Services.Helper;
using fiskaltrust.AndroidLauncher.Services.Queue;
using fiskaltrust.AndroidLauncher.Services.SCU;
using fiskaltrust.AndroidLauncher.Signing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareProvider
    {
        private const string PACKAGE_NAME_DE_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_DE_SWISSBIT_CLOUD_V2 = "fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2";
        private const string PACKAGE_NAME_DE_FISKALY_CERTIFIED = "fiskaltrust.Middleware.SCU.DE.FiskalyCertified";
        private const string PACKAGE_NAME_IT_EPSON_RT_PRINTER = "fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter";
        private const string PACKAGE_NAME_IT_CUSTOM_RT_SERVER = "fiskaltrust.Middleware.SCU.IT.CustomRTServer";
        private const string PACKAGE_NAME_IT_CUSTOM_RT_PRINTER = "fiskaltrust.Middleware.SCU.IT.CustomRTPrinter";

        private readonly ftCashBoxConfiguration _configuration;

        private readonly Guid _cashboxId;
        public Guid CashboxId => _cashboxId;
        private readonly string _accessToken;
        public string AccessToken => _accessToken;
        private readonly bool _isSandbox;
        private readonly LogLevel _logLevel;

        private Android.OS.PowerManager.WakeLock _wakeLock;
        private List<IHelper> _helpers;
        private IPOS _poss;
        private AbstractScuList _scus;

        public IPOS POS => _poss;

        public string CountryCode { get; set; }

        public Api.PosSystem.Core.Interfaces.IMiddlewareClient MiddlewareClientAndroid => new MiddlewareClientAndroid(_poss, CountryCode);

        public PackageConfiguration QueueConfiguration { get; private set; }

        public MiddlewareProvider(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration, bool isSandbox, LogLevel logLevel)
        {
            _configuration = configuration;
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;
            _logLevel = logLevel;

            _helpers = new List<IHelper>();
            _scus = new AbstractScuList();
        }

        public async Task StartAsync()
        {
            foreach (var scuConfig in _configuration.ftSignaturCreationDevices)
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
                    case PACKAGE_NAME_DE_SWISSBIT_CLOUD_V2:
                        await InitializeDESwissbitCloudV2ScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_IT_CUSTOM_RT_SERVER:
                        await InitializeITCustomRTServerScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_IT_CUSTOM_RT_PRINTER:
                        await InitializeITCustomRTPrinterScuAsync(scuConfig);
                        break;
                    default:
                        throw new ArgumentException($"The Android launcher currently only supports the following SCU packages: {PACKAGE_NAME_DE_SWISSBIT}, {PACKAGE_NAME_DE_SWISSBIT_CLOUD_V2}, {PACKAGE_NAME_DE_FISKALY_CERTIFIED}, {PACKAGE_NAME_IT_EPSON_RT_PRINTER}, {PACKAGE_NAME_IT_CUSTOM_RT_SERVER}, {PACKAGE_NAME_IT_CUSTOM_RT_PRINTER}.");
                }
            }

            if (_configuration.ftQueues.Count() != 1)
            {
                throw new ArgumentException("The Android launcher currently only supports exactly one queue package.");
            }

            foreach (var queueConfig in _configuration.ftQueues)
            {
                queueConfig.Configuration["sandbox"] = _isSandbox;
                await InitializeQueueAsync(queueConfig);
            }

            await InitializeHelipadHelperAsync(_configuration);
        }

        public async Task StopAsync()
        {
            // MiddlewareProvider doesn't manage hosts directly, just helpers and wake locks
            foreach (var helper in _helpers)
            {
                helper.StopBegin();
                helper.StopEnd();
            }

            _wakeLock?.Release();
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
            Log.Logger.Debug($"Created Italian SCU of type 'fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter'.");
        }

        private async Task InitializeDESwissbitCloudV2ScuAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new DESwissbitCloudV2ScuProvider();
            var scu = scuProvider.CreateSCU(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created German SCU of type 'fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2'.");
        }

        private async Task InitializeITCustomRTServerScuAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new ITCustomRTServerScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created Italian SCU of type 'fiskaltrust.Middleware.SCU.IT.CustomRTServer'.");
        }

        private async Task InitializeITCustomRTPrinterScuAsync(PackageConfiguration packageConfig)
        {
            var scuProvider = new ITCustomRTPrinterScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            _scus.Add(GetPrimaryUriForSignaturCreationUnit(packageConfig), scu);
            Log.Logger.Debug($"Created Italian SCU of type 'fiskaltrust.Middleware.SCU.IT.CustomRTPrinter'.");
        }

        private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        {
            var queueProvider = new SQLiteQueueProvider();
            var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _accessToken, _isSandbox, _logLevel, _scus));
            _poss = pos;
            var queues = ParseParameter<List<ftQueue>>(packageConfig.Configuration, "init_ftQueue") ?? new List<ftQueue>();
            QueueConfiguration = packageConfig;
            CountryCode = queues.FirstOrDefault()?.CountryCode?.ToUpper();
            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.Queue.SQLite' is listening on 'Intnet Interface'.");
        }

        private T ParseParameter<T>(Dictionary<string, object> config, string key) where T : new()
        {
            T parameter;
            if (config.ContainsKey(key))
            {
                parameter = JsonConvert.DeserializeObject<T>(config[key].ToString());
            }
            else
            {
                return default; // Sometimes we don't get that data. We just expect it to be empty then.
            }
            return parameter;
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
