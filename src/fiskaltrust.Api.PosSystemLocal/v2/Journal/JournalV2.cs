using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Api.PosSystemLocal.OperationHandling;
using fiskaltrust.Api.PosSystemLocal.v2;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Interfaces;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace fiskaltrust.Api.POS.v2.Journal;

public static class JournalV2
{
    public static async Task<(byte[]?, string? contentType)> Journal(IMiddlewareClient middlewareClient, JournalRequest journalRequest)
    {
        var response = await middlewareClient.JournalV2Async(journalRequest);
        if (response.error != null)
        {
            throw new Exception(response.error);
        }

        var content = Encoding.UTF8.GetString(response.Item1);
        if (content.StartsWith("<?xml"))
            return (response.Item1, contentType: "application/xml");
        return (response.Item1, contentType: "application/json");
    }

    public static async Task<OperationItem?> JournalOperationItem(IAsyncStorageBootstrapper asyncStorageBootstrapper, Guid operationId, OperationStateMachine operationStateMachine, PackageConfiguration packageConfiguration)
    {
        var operationItem = await operationStateMachine.State(operationId);
        if (operationItem == null)
        {
            return null;
        }
        return operationItem;
    }

    public static async Task<OperationItem?> PeekJournalItemOperationItem(IAsyncStorageBootstrapper asyncStorageBootstrapper, Guid operationId, OperationStateMachine operationStateMachine, PackageConfiguration packageConfiguration)
    {
        var operationItem = await operationStateMachine.State(operationId);
        if (operationItem == null)
        {
            return null;
        }
        return operationItem;
    }

    public static async Task<IAsyncEnumerable<OperationItem>> PeekJournalItemOperationItems(IAsyncStorageBootstrapper asyncStorageBootstrapper, PackageConfiguration packageConfiguration)
    {
        var services = new ServiceCollection();
        await asyncStorageBootstrapper.ConfigureStorageServicesAsync(packageConfiguration, services);
        var operationItemRepository = services.BuildServiceProvider().GetRequiredService<IOperationItemRepository>();
        return operationItemRepository.GetAsyncEnumerable();
    }
}
