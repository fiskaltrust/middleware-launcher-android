using fiskaltrust.Middleware.Storage.AzureTableStorage.Interfaces;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class InMemoryStorageBootstrapper : IAsyncStorageBootstrapper
    {
        public async Task ConfigureStorageServicesAsync(PackageConfiguration configuration, IServiceCollection services)
        {
            var queueConfiguration = new QueueConfiguration { QueueId = configuration.Id };

            services.AddSingleton(queueConfiguration);
            services.AddSingleton<IOperationItemRepository, InMemoryOperationItemRepository>();
        }
    }
}
