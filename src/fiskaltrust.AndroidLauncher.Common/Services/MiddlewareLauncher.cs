using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services.Configuration;
using fiskaltrust.AndroidLauncher.Common.Services.Helper;
using fiskaltrust.AndroidLauncher.Common.Services.Queue;
using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FiskalyV1SCUConfiguration = fiskaltrust.Middleware.SCU.DE.Fiskaly.FiskalySCUConfiguration;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    internal class MiddlewareLauncher
    {
        private const string PACKAGE_NAME_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_FISKALY = "fiskaltrust.Middleware.SCU.DE.Fiskaly";
        private const string PACKAGE_NAME_FISKALY_CERTIFIED = "fiskaltrust.Middleware.SCU.DE.FiskalyCertified";
      
        private readonly IHostFactory _hostFactory;
        private readonly IUrlResolver _urlResolver;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILocalConfigurationProvider _localConfigurationProvider;

        private readonly Guid _cashboxId;
        private readonly string _accessToken;
        private readonly bool _isSandbox;
        private readonly Dictionary<string, object> _scuParams;
        private readonly LogLevel _logLevel;
        
        private List<IHelper> _helpers;
        private IHost<IPOS> _posHost;
        private IHost<IDESSCD> _scuHost;

        private string _defaultUrl = null;

        public bool IsRunning { get; set; }

        public MiddlewareLauncher(IHostFactory hostFactory, IUrlResolver urlResolver, Guid cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> scuParams)
        {
            _hostFactory = hostFactory;
            _urlResolver = urlResolver;

            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;
            _scuParams = scuParams;
            _logLevel = logLevel;

            _configurationProvider = new HelipadConfigurationProvider();
            _localConfigurationProvider = new LocalConfigurationProvider();
            _helpers = new List<IHelper>();
        }

        public async Task StartAsync()
        {
            _posHost = _hostFactory.CreatePosHost();
            _scuHost = _hostFactory.CreateDeSscdHost();

            ftCashBoxConfiguration configuration;
            try
            {
                configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken, _isSandbox);
                await _localConfigurationProvider.PersistAsync(_cashboxId, _accessToken, configuration);
            }
            catch (Exception)
            {
                configuration = await _localConfigurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken, _isSandbox);
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
                        if (_scuParams.TryGetValue(nameof(FiskalyV1SCUConfiguration.FiskalyClientTimeout), out var clientTimeout)) scuConfig.Configuration[nameof(FiskalyV1SCUConfiguration.FiskalyClientTimeout)] = clientTimeout;
                        if (_scuParams.TryGetValue(nameof(FiskalyV1SCUConfiguration.FiskalyClientSmaersTimeout), out var smaersTimeout)) scuConfig.Configuration[nameof(FiskalyV1SCUConfiguration.FiskalyClientSmaersTimeout)] = smaersTimeout;

                        await InitializeFiskalyScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_FISKALY_CERTIFIED:
                        await InitializeFiskalyCertifiedScuAsync(scuConfig);
                        break;
                    default:
                        throw new ArgumentException($"The Android launcher currently only supports the following SCU packages: {PACKAGE_NAME_SWISSBIT}, {PACKAGE_NAME_FISKALY}.");
                }
            }

            foreach (var queueConfig in configuration.ftQueues)
            {
                queueConfig.Configuration["sandbox"] = _isSandbox;

                if (_defaultUrl == null)
                    _defaultUrl = _urlResolver.GetProtocolSpecificUrl(queueConfig);
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

            foreach (var helper in _helpers)
            {
                helper.StopBegin();
                helper.StopEnd();
            }

            IsRunning = false;
        }

        public async Task<IPOS> GetPOS()
        {
            return await _posHost.GetProxyAsync();
        }

        private async Task InitializeSwissbitScuAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new SwissbitScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _logLevel);
            await _scuHost.StartAsync(url, scu, _logLevel);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.SCU.DE.Swissbit' is listening on '{url}'.");
        }

        private async Task InitializeFiskalyScuAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new FiskalyScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _logLevel);
            await _scuHost.StartAsync(url, scu, _logLevel);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.SCU.DE.Fiskaly' is listening on '{url}'.");
        }

        private async Task InitializeFiskalyCertifiedScuAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new FiskalyCertifiedScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _logLevel);
            await _scuHost.StartAsync(url, scu, _logLevel);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.SCU.DE.FiskalyCertified' is listening on '{url}'.");
        }

        private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var queueProvider = new SQLiteQueueProvider();
            var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _logLevel, _scuHost));

            await _posHost.StartAsync(url, pos, _logLevel);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.Queue.SQLite' is listening on '{url}'.");
        }

        private async Task InitializeHelipadHelperAsync(ftCashBoxConfiguration configuration)
        {
            var helipadHelperProvider = new HelipadHelperProvider();
            var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(configuration, _accessToken, _isSandbox, _logLevel, _posHost));
            helper.StartBegin();
            helper.StartEnd();

            _helpers.Add(helper);
        }
    }
}