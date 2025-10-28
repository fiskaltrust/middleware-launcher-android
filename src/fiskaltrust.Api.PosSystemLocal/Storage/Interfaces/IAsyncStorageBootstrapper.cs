using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage;

public interface IAsyncStorageBootstrapper
{
    public Task ConfigureStorageServicesAsync(PackageConfiguration configuration, IServiceCollection services);
}
