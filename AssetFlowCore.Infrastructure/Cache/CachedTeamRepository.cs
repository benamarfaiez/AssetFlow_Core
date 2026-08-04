using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedTeamRepository(ITeamRepository innerRepository, IMemoryCache memoryCache) : ITeamRepository
{
    private const string TeamsListCacheKey = CacheKeys.TeamsList;
    private readonly ITeamRepository _inner = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
    private readonly IMemoryCache _memory = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private static MemoryCacheEntryOptions CacheOptions() => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _memory.GetOrCreateAsync(GetIdKey(id), async entry =>
        {
            entry.SetOptions(CacheOptions());
            return await _inner.GetByIdAsync(id, cancellationToken);
        });

    public async Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Tente de récupérer depuis la liste globale en cache
        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list != null)
        {
            var found = list.FirstOrDefault(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
        }

        // Cache Miss : Récupération bdd
        var team = await _inner.GetByNameAsync(name, cancellationToken);
        if (team != null)
        {
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
        }
        return team;
    }

    public Task<IEnumerable<Team>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return _memory.GetOrCreateAsync(TeamsListCacheKey, async entry =>
        {
            entry.SetOptions(CacheOptions());
            var teams = await _inner.GetAllActiveAsync(cancellationToken);
            return teams ?? [];
        })!;
    }

    public Task<IEnumerable<Team>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _memory.GetOrCreateAsync(CacheKeys.TeamsListAll, async entry =>
        {
            entry.SetOptions(CacheOptions());
            var teams = await _inner.GetAllAsync(cancellationToken);
            return teams ?? [];
        })!;
    }

    public async Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetType) || string.IsNullOrWhiteSpace(criticality)) return null;

        // Tente de récupérer depuis la liste globale en cache
        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list != null)
        {
            var found = list.FirstOrDefault(t =>
                string.Equals(t.AssetType, assetType.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.TicketCriticality.ToString(), criticality.Trim(), StringComparison.OrdinalIgnoreCase));

            if (found != null) return found;
        }

        // Cache Miss
        var team = await _inner.GetByAssetTypeAndCriticalityAsync(assetType, criticality, cancellationToken);
        if (team != null)
        {
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
        }
        return team;
    }

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        await _inner.AddAsync(team, cancellationToken);

        InvalidateLists();
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());
    }

    public async Task UpdateAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        await _inner.UpdateAsync(team, cancellationToken);

        InvalidateLists();
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());
    }

    public async Task RemoveAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        await _inner.RemoveAsync(team, cancellationToken);

        _memory.Remove(GetIdKey(team.Id));
        InvalidateLists();
    }

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list != null)
        {
            return list.Any(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return await _inner.ExistsWithNameAsync(name, cancellationToken);
    }

    public void RefreshCacheFor(Team team)
    {
        if (team == null) return;
        InvalidateLists();
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());
    }

    /// <summary>
    /// Toute écriture sur une équipe périme les deux listes : la liste des actives et la liste
    /// complète. En oublier une servirait un référentiel d'équipes obsolète pendant 5 minutes.
    /// </summary>
    private void InvalidateLists()
    {
        _memory.Remove(TeamsListCacheKey);
        _memory.Remove(CacheKeys.TeamsListAll);
    }

    private static string GetIdKey(Guid id) => CacheKeys.Team(id);
}
