using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models.ifPOS.v2;


namespace fiskaltrust.AndroidLauncher.Storage
{
    public class InMemoryOperationItemRepository : BaseInMemoryRepository<Guid, OperationItem>, IOperationItemRepository
    {
        public InMemoryOperationItemRepository(QueueConfiguration queueConfig)
            : base(queueConfig, TABLE_NAME)
        {
        }

        public const string TABLE_NAME = "OperationItem";

        protected override void EntityUpdated(OperationItem entity) 
        {
            entity.TimeStamp = DateTime.UtcNow;
        }

        protected override Guid GetIdForEntity(OperationItem entity) 
        {
            return entity.cbOperationItemID;
        }

        public async Task InsertOrUpdateAsync(OperationItem storageEntity)
        {
            EntityUpdated(storageEntity);
            var key = GetIdForEntity(storageEntity);
            _storage.AddOrUpdate(key, storageEntity, (k, v) => storageEntity);
            await Task.CompletedTask;
        }

        public override async Task<OperationItem?> GetAsync(Guid operationId)
        {
            return await Task.FromResult(_storage.TryGetValue(operationId, out var entity) ? entity : null);
        }       
    }
}