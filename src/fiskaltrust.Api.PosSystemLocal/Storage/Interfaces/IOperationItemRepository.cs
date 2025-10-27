using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Interfaces
{
    public interface IOperationItemRepository
    {
        public Task<IEnumerable<OperationItem>> GetAsync();
        public IAsyncEnumerable<OperationItem> GetAsyncEnumerable();
        public Task<OperationItem?> GetAsync(Guid operationId);
        public Task InsertAsync(OperationItem storageEntity);
        public Task InsertOrUpdateAsync(OperationItem storageEntity);
    }
}

