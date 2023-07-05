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
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    internal class MiddlewareLauncher
    {
        private const string PACKAGE_NAME_DE_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_DE_FISKALY = "fiskaltrust.Middleware.SCU.DE.Fiskaly";
        private const string PACKAGE_NAME_DE_FISKALY_CERTIFIED = "fiskaltrust.Middleware.SCU.DE.FiskalyCertified";
        private const string PACKAGE_NAME_IT_EPSON = "fiskaltrust.Middleware.SCU.IT.Epson";

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
        private List<IHost<IPOS>> _posHosts;
        private List<IHost<SSCD>> _scuHosts;

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
                    case PACKAGE_NAME_DE_SWISSBIT:
                        await InitializeDESwissbitScuAsync(scuConfig);
                        break;                  
                    case PACKAGE_NAME_DE_FISKALY_CERTIFIED:
                        await InitializeDEFiskalyCertifiedScuAsync(scuConfig);
                        break;
                    case PACKAGE_NAME_IT_EPSON:
                        await InitializeITEpsonScuAsync(scuConfig);
                        break;
                    default:
                        throw new ArgumentException($"The Android launcher currently only supports the following SCU packages: {PACKAGE_NAME_DE_SWISSBIT}, {PACKAGE_NAME_DE_FISKALY_CERTIFIED}, {PACKAGE_NAME_IT_EPSON}.");
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
            if (_posHosts != null)
            {
                await Task.WhenAll(_posHosts.Select(h => h.StopAsync())).ConfigureAwait(false);
            }
            if (_scuHosts != null)
            {
                await Task.WhenAll(_scuHosts.Select(h => h.StopAsync())).ConfigureAwait(false);
            }

            foreach (var helper in _helpers)
            {
                helper.StopBegin();
                helper.StopEnd();
            }

            IsRunning = false;
        }

        public async IAsyncEnumerable<IPOS> GetPOSs()
        {
            foreach (var host in _posHosts)
            {
                yield return await host.GetProxyAsync();
            }
        }

        private async Task InitializeDESwissbitScuAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new DESwissbitScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            var host = _hostFactory.CreateSscdHost<DESSCD>();
            _scuHosts.Add(host);
            await host.StartAsync(url, scu, _logLevel);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.SCU.DE.Swissbit' is listening on '{url}'.");
        }

        private async Task InitializeDEFiskalyCertifiedScuAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new DEFiskalyCertifiedScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            var host = _hostFactory.CreateSscdHost<DESSCD>();
            _scuHosts.Add(host);
            await host.StartAsync(url, scu, _logLevel).ConfigureAwait(false);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.SCU.DE.FiskalyCertified' is listening on '{url}'.");
        }

        private async Task InitializeITEpsonScuAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var scuProvider = new ITEpsonScuProvider();
            var scu = scuProvider.CreateSCU(packageConfig, _cashboxId, _isSandbox, _logLevel);
            var host = _hostFactory.CreateSscdHost<ITSSCD>();
            _scuHosts.Add(host);
            await host.StartAsync(url, scu, _logLevel).ConfigureAwait(false);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.SCU.DE.FiskalyCertified' is listening on '{url}'.");
        }

        private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        {
            string url = _urlResolver.GetProtocolSpecificUrl(packageConfig);

            var queueProvider = new SQLiteQueueProvider();
            var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _isSandbox, _logLevel, _scuHosts));
            var host = _hostFactory.CreatePosHost();
            _posHosts.Add(host);
            await host.StartAsync(url, pos, _logLevel).ConfigureAwait(false);

            Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.Queue.SQLite' is listening on '{url}'.");
        }

        private async Task InitializeHelipadHelperAsync(ftCashBoxConfiguration configuration)
        {
            var helipadHelperProvider = new HelipadHelperProvider();
            var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(configuration, _accessToken, _isSandbox, _logLevel, _posHosts));
            helper.StartBegin();
            helper.StartEnd();

            _helpers.Add(helper);
        }
    }
}