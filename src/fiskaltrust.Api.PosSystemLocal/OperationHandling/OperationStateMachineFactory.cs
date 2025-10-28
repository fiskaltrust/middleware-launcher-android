using fiskaltrust.Api.PosSystemLocal.v2;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Interfaces;
using fiskaltrust.storage.serialization.V0;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Api.PosSystemLocal.OperationHandling;

public class OperationStateMachineFactory(ILoggerFactory loggerFactory)
{
    public async Task<OperationStateMachine> CreateAsync(PackageConfiguration queueConfiguration, IMiddlewareClient middlewareClient)
    { 
        var services = new ServiceCollection();
        await new InMemoryStorageBootstrapper().ConfigureStorageServicesAsync(queueConfiguration, services);
        return new OperationStateMachine(loggerFactory.CreateLogger<OperationStateMachine>(), middlewareClient, queueConfiguration.Id, loggerFactory, services.BuildServiceProvider().GetRequiredService<IOperationItemRepository>());
    }
}

