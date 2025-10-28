using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.AzureTableStorage;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public abstract class BaseInMemoryRepository<TKey, TStorageEntity>
        where TStorageEntity : class
        where TKey : notnull
    {
        protected readonly ConcurrentDictionary<TKey, TStorageEntity> _storage;
        protected readonly string _storageEntityName;

        public BaseInMemoryRepository(QueueConfiguration queueConfig, string storageEntityName)
        {
            _storage = new ConcurrentDictionary<TKey, TStorageEntity>();
            _storageEntityName = storageEntityName;
        }

        public virtual async Task<IEnumerable<TStorageEntity>> GetAsync()
        {
            return await Task.FromResult(_storage.Values.ToList());
        }

        public virtual IAsyncEnumerable<TStorageEntity> GetAsyncEnumerable()
        {
            return _storage.Values.ToAsyncEnumerable();
        }

        public virtual async Task<TStorageEntity?> GetAsync(TKey id)
        {
            return await Task.FromResult(_storage.TryGetValue(id, out var entity) ? entity : null);
        }

        public virtual async Task InsertAsync(TStorageEntity storageEntity)
        {
            EntityUpdated(storageEntity);
            var key = GetIdForEntity(storageEntity);
            _storage.AddOrUpdate(key, storageEntity, (k, v) => storageEntity);
            await Task.CompletedTask;
        }

        public async Task<TStorageEntity?> RemoveAsync(TKey key)
        {
            var removed = _storage.TryRemove(key, out var entity);
            return await Task.FromResult(removed ? entity : null);
        }

        protected abstract TKey GetIdForEntity(TStorageEntity entity);

        protected abstract void EntityUpdated(TStorageEntity entity);
    }
}