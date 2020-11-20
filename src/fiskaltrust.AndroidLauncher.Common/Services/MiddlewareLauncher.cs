using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services.Configuration;
using fiskaltrust.AndroidLauncher.Common.Services.Helper;
using fiskaltrust.AndroidLauncher.Common.Services.Queue;
using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Fiskaly;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    internal class MiddlewareLauncher
    {
        private const string PACKAGE_NAME_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_FISKALY = "fiskaltrust.Middleware.SCU.DE.Fiskaly";

        private readonly Guid _cashboxId;
        private readonly string _accessToken;
        private readonly bool _isSandbox;
        private readonly Dictionary<string, object> _scuParams;
        private readonly LogLevel _logLevel;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILocalConfigurationProvider _localConfigurationProvider;

        private IHost<IPOS> _posHost;
        private IHost<IDESSCD> _scuHost;

        private string _defaultUrl = null;

        public bool IsRunning { get; set; }

        public MiddlewareLauncher(Guid cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> scuParams)
        {
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;
            _scuParams = scuParams;
            _logLevel = logLevel;

            _configurationProvider = new HelipadConfigurationProvider();
            _localConfigurationProvider = new LocalConfigurationProvider();
        }

        public async Task StartAsync()
        {
            var hostFactory = ServiceLocator.Resolve<IHostFactory>();

            _posHost = hostFactory.CreatePosHost();
            _scuHost = hostFactory.CreateDeSscdHost();

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
                        if (_scuParams.TryGetValue(nameof(FiskalySCUConfiguration.FislayClientTimeout), out var clientTimeout)) scuConfig.Configuration[nameof(FiskalySCUConfiguration.FislayClientTimeout)] = clientTimeout;
                        if (_scuParams.TryGetValue(nameof(FiskalySCUConfiguration.FislayClientSmaersTimeout), out var smaersTimeout)) scuConfig.Configuration[nameof(FiskalySCUConfiguration.FislayClientSmaersTimeout)] = smaersTimeout;

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
                    _defaultUrl = ServiceLocator.Resolve<IUrlResolver>().GetProtocolSpecificUrl(queueConfig);
                await InitializeQueueAsync(queueConfig);
            }

            await InitializeHelipadHelperAsync(configuration);

            IsRunning = true;
        }

        public async Task StopAsync()
        {
            if (_posHost != null)
            {
                await _posHost.StopAsync();
            }
            if (_scuHost != null)
            {
                await _scuHost.StopAsync();
            }

            IsRunning = false;
        }

        public async Task<IPOS> GetPOS()
        {
            return await _posHost.GetProxyAsync();
        }

        private async Task InitializeSwissbitScuAsync(PackageConfiguration packageConfig)
        {
            string url = ServiceLocator.Resolve<IUrlResolver>().GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new SwissbitScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _logLevel);
            await _scuHost.StartAsync(url, scu);
        }

        private async Task InitializeFiskalyScuAsync(PackageConfiguration packageConfig)
        {
            string url = ServiceLocator.Resolve<IUrlResolver>().GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new FiskalyScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _logLevel);
            await _scuHost.StartAsync(url, scu);
        }

        private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        {
            string url = ServiceLocator.Resolve<IUrlResolver>().GetProtocolSpecificUrl(packageConfig);

            var queueProvider = new SQLiteQueueProvider();
            var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _logLevel, _scuHost));

            await _posHost.StartAsync(url, pos);
        }

        private async Task InitializeHelipadHelperAsync(ftCashBoxConfiguration configuration)
        {
            var helipadHelperProvider = new HelipadHelperProvider();
            var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(configuration, _accessToken, _isSandbox, _logLevel, _posHost));
            helper.StartBegin();
            helper.StartEnd();
        }
    }
}