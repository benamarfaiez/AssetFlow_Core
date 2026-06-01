using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedAssetRepository(IAssetRepository innerRepository, IMemoryCache memoryCache) : IAssetRepository
{
    private const string AssetListCacheKey = "Assets_List_ReadOnly";

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync()
    {
        return await memoryCache.GetOrCreateAsync(AssetListCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await innerRepository.GetAllReadOnlyAsync();
        }) ?? [];
    }

    // Exclusion du cache pour les opérations transactionnelles ou mutables
    public Task<Asset?> GetByIdAsync(Guid id) => innerRepository.GetByIdAsync(id);
    public Task<bool> ExistsWithSerialNumberAsync(string serialNumber) => innerRepository.ExistsWithSerialNumberAsync(serialNumber);
    public Task AddAsync(Asset asset) => innerRepository.AddAsync(asset);
}