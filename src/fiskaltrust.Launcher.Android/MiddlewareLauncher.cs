using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Services.Helpers;
using fiskaltrust.AndroidLauncher.Services.Queue;
using fiskaltrust.AndroidLauncher.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher
{
    public class MiddlewareLauncher
    {
        private const string PACKAGE_NAME_SWISSBIT = "fiskaltrust.Middleware.SCU.DE.Swissbit";
        private const string PACKAGE_NAME_FISKALY = "fiskaltrust.Middleware.SCU.DE.Fiskaly";

        private readonly Guid _cashboxId;
        private readonly string _accessToken;
        private IConfigurationProvider _configurationProvider;
        private readonly GrpcHost _posHost;
        private readonly GrpcHost _scuHost;

        private string _defaultUrl = null;

        public MiddlewareLauncher(Guid cashboxId, string accessToken)
        {
            _cashboxId = cashboxId;
            _accessToken = accessToken;

            _configurationProvider = new HelipadConfigurationProvider();
            _posHost = new GrpcHost();
            _scuHost = new GrpcHost();
        }

        public async Task StartAsync()
        {
            var configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId, _accessToken);

            foreach (var scuConfig in configuration.ftSignaturCreationDevices)
            {
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
                if (_defaultUrl == null)
                    _defaultUrl = GetGrpcUrl(queueConfig);
                await InitializeQueueAsync(queueConfig);
            }

            await InitializeHelipadHelperAsync(configuration);
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
            var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(configuration, _accessToken));
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