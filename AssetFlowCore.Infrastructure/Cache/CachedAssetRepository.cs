using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedAssetRepository(IAssetRepository innerRepository, IMemoryCache memoryCache) : IAssetRepository
{
    private const string AssetListCacheKey = "Assets_List_ReadOnly";
    private readonly IAssetRepository _inner = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
    private readonly IMemoryCache _memory = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private MemoryCacheEntryOptions CacheOptions() => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync()
    {
        return await _memory.GetOrCreateAsync(AssetListCacheKey, async entry =>
        {
            entry.SetOptions(CacheOptions());
            return await _inner.GetAllReadOnlyAsync();
        }) ?? [];
    }

    // Cache les assets individuels (inclut les Tickets via le repo inner)
    public Task<Asset?> GetByIdAsync(Guid id)
        => _memory.GetOrCreateAsync(GetIdKey(id), async entry =>
        {
            entry.SetOptions(CacheOptions());
            var asset = await _inner.GetByIdAsync(id);
            return asset;
        });

    public Task<bool> ExistsWithSerialNumberAsync(string serialNumber)
        => _inner.ExistsWithSerialNumberAsync(serialNumber.ToUpper().Trim());

    public async Task AddAsync(Asset asset)
    {
        if (asset != null)
        {
            await _inner.AddAsync(asset);

            // Refresh cache for the new asset and invalidate list cache
            _memory.Set(GetIdKey(asset.Id), asset, CacheOptions());
            _memory.Remove(AssetListCacheKey);
        }
        else
            throw new ArgumentNullException(nameof(asset));
    }

    private static string GetIdKey(Guid id) => $"asset_{id:N}";
}
