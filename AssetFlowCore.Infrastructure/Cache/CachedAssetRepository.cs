using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedAssetRepository(IAssetRepository innerRepository, IMemoryCache memoryCache) : IAssetRepository
{
    private readonly IAssetRepository _inner = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
    private readonly IMemoryCache _memory = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private static MemoryCacheEntryOptions CacheOptions() => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default)
    {
        return await _memory.GetOrCreateAsync(CacheKeys.AssetsList, async entry =>
        {
            entry.SetOptions(CacheOptions());
            return await _inner.GetAllReadOnlyAsync(cancellationToken);
        }) ?? [];
    }

    // Volontairement sans cache : tous les appelants de GetByIdAsync sont des cas d'usage d'écriture
    // (ouverture, prise en charge, clôture d'incident, mise au rebut) qui mutent l'actif retourné.
    // Servir une instance mise en cache — donc détachée du DbContext de la requête courante —
    // ferait échouer silencieusement la persistance de ces mutations.
    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _inner.GetByIdAsync(id, cancellationToken);

    // Sans cache également : la fiche agrège les incidents de l'actif, dont l'état change à
    // chaque étape du cycle de vie d'un incident sans passer par une écriture sur l'actif.
    public Task<Asset?> GetByIdWithTicketsAsync(Guid id, CancellationToken cancellationToken = default)
        => _inner.GetByIdWithTicketsAsync(id, cancellationToken);

    public Task<bool> ExistsWithSerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        => _inner.ExistsWithSerialNumberAsync(serialNumber.ToUpper().Trim(), cancellationToken);

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        await _inner.AddAsync(asset, cancellationToken);
        _memory.Remove(CacheKeys.AssetsList);
    }
}
