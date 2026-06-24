using fiskaltrust.AndroidLauncher.Activitites;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.AndroidLauncher.Storage;
using fiskaltrust.Api.PosSystem.Core;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Services.POSSystemApiCore
{

    public class POSSystemApiCoreProvider
    {
        private readonly ILocalConfigurationProvider _localConfigurationProvider;

        public POSSystemApiCoreProvider()
        {
            _localConfigurationProvider = new LocalConfigurationProvider();
        }
        public async Task<PosSystemApiCore> CreateAsync(Guid CashBoxId, string AccessToken, bool IsSandbox)
        {
            
            var configuration = await _localConfigurationProvider.GetCashboxConfigurationAsync(CashBoxId, AccessToken, IsSandbox).ConfigureAwait(true);
            var config = new POSSystemApiCoreConfiguration
            {
                CashBoxId = CashBoxId,
                AccessToken = AccessToken,
                Configuration = JsonConvert.SerializeObject(configuration),
                AppEnvironment = IsSandbox ? AppEnvironments.Sandbox : AppEnvironments.Production,
                LauncherEnvironment = LauncherEnvironments.Local
            };

            var bootstrapper = new POSCoreBootstrapper();
            var json = JsonConvert.SerializeObject(config);
            bootstrapper.Configuration = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            var services = new ServiceCollection();
            await bootstrapper.ConfigureServices(services);

            services.AddSingleton<IMiddlewareClient>(_ =>PosSystemAPIActivity.LocalMiddlewareServiceInstance.MiddlewareClientAndroid);           
            services.AddSingleton<IOperationItemRepository, MemoryOperationItemRepository>();
            services.AddSingleton<IStorageFactory, StorageFactory>();

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<PosSystemApiCore>();

        }

    }
}