using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedAssetRepository : IAssetRepository
{
    private readonly IAssetRepository _innerRepository;
    private readonly IMemoryCache _memoryCache;
    private const string AssetListCacheKey = "Assets_List_ReadOnly";

    public CachedAssetRepository(IAssetRepository innerRepository, IMemoryCache memoryCache)
    {
        _innerRepository = innerRepository;
        _memoryCache = memoryCache;
    }

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync()
    {
        return await _memoryCache.GetOrCreateAsync(AssetListCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _innerRepository.GetAllReadOnlyAsync();
        }) ?? Array.Empty<Asset>();
    }

    // Exclusion du cache pour les opérations transactionnelles ou mutables
    public Task<Asset?> GetByIdAsync(Guid id) => _innerRepository.GetByIdAsync(id);
    public Task<bool> ExistsWithSerialNumberAsync(string serialNumber) => _innerRepository.ExistsWithSerialNumberAsync(serialNumber);
    public Task AddAsync(Asset asset) => _innerRepository.AddAsync(asset);
}