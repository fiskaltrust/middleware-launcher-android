using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models.ifPOS.v2;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace fiskaltrust.AndroidLauncher.Storage;

public class MemoryOperationItemRepository : IOperationItemRepository
{
    private readonly IMemoryCache _cache;

    // Keeps track of stored keys for enumeration
    private readonly ConcurrentDictionary<Guid, byte> _keys = new();

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(1);

    public MemoryOperationItemRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(Guid operationId)
        => $"operation:{operationId}";

    public Task<IEnumerable<OperationItem>> GetAsync()
    {
        var result = new List<OperationItem>();

        foreach (var id in _keys.Keys)
        {
            if (_cache.TryGetValue(GetKey(id), out OperationItem? item) && item != null)
            {
                result.Add(item);
            }
        }

        return Task.FromResult<IEnumerable<OperationItem>>(result);
    }

    public Task<OperationItem?> GetAsync(Guid operationId)
    {
        _cache.TryGetValue(GetKey(operationId), out OperationItem? value);
        return Task.FromResult(value);
    }

    public async IAsyncEnumerable<OperationItem> GetAsyncEnumerable()
    {
        foreach (var id in _keys.Keys)
        {
            if (_cache.TryGetValue(GetKey(id), out OperationItem? item) && item != null)
            {
                yield return item;
            }

            await Task.Yield(); // keeps it truly async
        }
    }

    private MemoryCacheEntryOptions CreateEntryOptions(Guid operationId)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl
        };
        options.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            ((ConcurrentDictionary<Guid, byte>)state!).TryRemove(operationId, out _);
        }, _keys);
        return options;
    }

    public Task InsertAsync(OperationItem storageEntity)
    {
        var key = GetKey(storageEntity.cbOperationItemID);

        _cache.Set(key, storageEntity, CreateEntryOptions(storageEntity.cbOperationItemID));
        _keys[storageEntity.cbOperationItemID] = 0;

        return Task.CompletedTask;
    }

    public Task InsertOrUpdateAsync(OperationItem storageEntity)
    {
        var key = GetKey(storageEntity.cbOperationItemID);

        _cache.Set(key, storageEntity, CreateEntryOptions(storageEntity.cbOperationItemID));
        _keys[storageEntity.cbOperationItemID] = 0;

        return Task.CompletedTask;
    }
}
