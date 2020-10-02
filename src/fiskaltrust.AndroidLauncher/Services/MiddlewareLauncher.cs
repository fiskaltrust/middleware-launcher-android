using fiskaltrust.AndroidLauncher.Helpers.Hosting;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Services.Helper;
using fiskaltrust.AndroidLauncher.Services.Queue;
using fiskaltrust.AndroidLauncher.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.storage.serialization.V0;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services
{
    internal class MiddlewareLauncher
    {
        private const string PACKAGE_NAME_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_FISKALY = "fiskaltrust.Middleware.SCU.DE.Fiskaly";

        private readonly Guid _cashboxId;
        private readonly string _accessToken;
        private readonly bool _isSandbox;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILocalConfigurationProvider _localConfigurationProvider;

        private GrpcHost _posHost;
        private GrpcHost _scuHost;

        private string _defaultUrl = null;

        public bool IsRunning { get; set; }

        public MiddlewareLauncher(Guid cashboxId, string accessToken, bool isSandbox)
        {
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;

            _configurationProvider = new HelipadConfigurationProvider();
            _localConfigurationProvider = new LocalConfigurationProvider();
        }

        public async Task StartAsync()
        {
            _posHost = new GrpcHost();
            _scuHost = new GrpcHost();

            ftCashBoxConfiguration configuration;
            try
            {
                configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken);
                await _localConfigurationProvider.PersistAsync(_cashboxId, configuration);
            }
            catch (Exception)
            {
                configuration = await _localConfigurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken);
            }            

            foreach (var scuConfig in configuration.ftSignaturCreationDevices)
            {
                scuConfig.Configuration["sandbox"] = _isSandbox;
                switch (scuConfig.Package)
                {
                    case PACKAGE_NAME_SWISSBIT:
                        await InitializeSwissbitScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_FISKALY:
                        await InitializeFiskalyScuAsync(scuConfig);
                        break;
                    default:
                        throw new ArgumentException($"The Android launcher currently only supports the following SCU packages: {PACKAGE_NAME_SWISSBIT}, {PACKAGE_NAME_FISKALY}.");
                }
            }

            foreach (var queueConfig in configuration.ftQueues)
            {
                queueConfig.Configuration["sandbox"] = _isSandbox;

                if (_defaultUrl == null)
                    _defaultUrl = GetGrpcUrl(queueConfig);
                await InitializeQueueAsync(queueConfig);
            }

            await InitializeHelipadHelperAsync(configuration);

            IsRunning = true;
        }

        public async Task StopAsync()
        {
            if (_posHost != null)
            {
                await _posHost.ShutdownAsync();
            }
            if (_scuHost != null)
            {
                await _scuHost.ShutdownAsync();
            }

            IsRunning = false;
        }

        // If no URL is specified, the URL of the first queue is taken
        public async Task<IPOS> GetPOS(string url = null)
        {
            if (url == null)
            {
                url = _defaultUrl;
            }

            return await GrpcPosFactory.CreatePosAsync(new GrpcClientOptions
            {
                Url = new Uri(url)
            });
        }

        private Task InitializeSwissbitScuAsync(PackageConfiguration packageConfig)
        {
            string url = GetGrpcUrl(packageConfig);

            // Async initialization of the SCU is not possible, because JavaSystem.LoadLibrary("WormAPI") fails when not running on the UI thread..
            // TODO: E.g. move this call out of the Swissbit package and instead call it from here
            var scuProvider = new SwissbitScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig);
            _scuHost.StartService(url, scu);

            return Task.CompletedTask;
        }

        private async Task InitializeFiskalyScuAsync(PackageConfiguration packageConfig)
        {
            string url = GetGrpcUrl(packageConfig);

            var scuProvider = new FiskalyScuProvider();
            var scu = await Task.Run(() => scuProvider.CreateSCU(packageConfig));
            _scuHost.StartService(url, scu);
        }

        private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        {
            string url = GetGrpcUrl(packageConfig);

            var queueProvider = new SQLiteQueueProvider();
            var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig));

            _posHost.StartService(url, pos);
        }

        private async Task InitializeHelipadHelperAsync(ftCashBoxConfiguration configuration)
        {
            var helipadHelperProvider = new HelipadHelperProvider();
            var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(configuration, _accessToken, _isSandbox));
            helper.StartBegin();
            helper.StartEnd();
        }

        private static string GetGrpcUrl(PackageConfiguration packageConfig)
        {
            var url = packageConfig.Url.FirstOrDefault(x => x.StartsWith("grpc"));
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"At least one gRPC URL has to be set in the configuration of the {packageConfig.Package} package with the ID {packageConfig.Id}.");
            }

            return url;
        }
    }
}