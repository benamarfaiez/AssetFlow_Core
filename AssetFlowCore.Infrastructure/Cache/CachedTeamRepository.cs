using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedTeamRepository(ITeamRepository innerRepository, IMemoryCache memoryCache) : ITeamRepository
{
    private const string TeamsListCacheKey = "Teams_List_Active";
    private readonly ITeamRepository _inner = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
    private readonly IMemoryCache _memory = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private static MemoryCacheEntryOptions CacheOptions() => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _memory.GetOrCreateAsync(GetIdKey(id), async entry =>
        {
            entry.SetOptions(CacheOptions());
            return await _inner.GetByIdAsync(id, cancellationToken);
        });

    public async Task<Team?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Tente de récupérer depuis la liste globale en cache
        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list != null)
        {
            var found = list.FirstOrDefault(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
        }

        // Cache Miss : Récupération bdd
        var team = await _inner.GetByNameAsync(name);
        if (team != null)
        {
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
        }
        return team;
    }

    public Task<IEnumerable<Team>> GetAllActiveAsync()
    {
        return _memory.GetOrCreateAsync(TeamsListCacheKey, async entry =>
        {
            entry.SetOptions(CacheOptions());
            var teams = await _inner.GetAllActiveAsync();
            return teams ?? [];
        })!;
    }

    public async Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality)
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
        var team = await _inner.GetByAssetTypeAndCriticalityAsync(assetType, criticality);
        if (team != null)
        {
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
        }
        return team;
    }

    public async Task AddAsync(Team team)
    {
        ArgumentNullException.ThrowIfNull(team);

        await _inner.AddAsync(team);

        _memory.Remove(TeamsListCacheKey);
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());
    }

    public async Task UpdateAsync(Team team)
    {
        ArgumentNullException.ThrowIfNull(team);

        await _inner.UpdateAsync(team);

        _memory.Remove(TeamsListCacheKey);
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());
    }

    public async Task RemoveAsync(Team team)
    {
        ArgumentNullException.ThrowIfNull(team);

        await _inner.RemoveAsync(team);

        _memory.Remove(GetIdKey(team.Id));
        _memory.Remove(TeamsListCacheKey);
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list != null)
        {
            return list.Any(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return await _inner.ExistsWithNameAsync(name);
    }

    public void RefreshCacheFor(Team team)
    {
        if (team == null) return;
        _memory.Remove(TeamsListCacheKey);
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());
    }

    private static string GetIdKey(Guid id) => $"team_{id:N}";
}