using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Cache;

public class CachedTeamRepository(ITeamRepository innerRepository, IMemoryCache memoryCache, TimeSpan? expiration = null) : ITeamRepository
{
    private const string TeamsListCacheKey = "Teams_List_Active";
    private readonly TimeSpan _expiration = expiration ?? TimeSpan.FromMinutes(5);
    private readonly ITeamRepository _inner = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
    private readonly IMemoryCache _memory = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private MemoryCacheEntryOptions CacheOptions() => new() { AbsoluteExpirationRelativeToNow = _expiration };

    public Task<Team?> GetByIdAsync(Guid id)
        => _memory.GetOrCreateAsync(GetIdKey(id), async entry =>
        {
            entry.SetOptions(CacheOptions());
            var team = await _inner.GetByIdAsync(id);
            return team;
        });

    public async Task<Team?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list))
        {
            var found = list?.FirstOrDefault(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                return found;
            }
        }

        var team = await _inner.GetByNameAsync(name);
        if (team != null)
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
        return team;
    }

    public Task<IEnumerable<Team>?> GetAllActiveAsync()
    {
        return _memory.GetOrCreateAsync(TeamsListCacheKey, async entry =>
            {
                entry.SetOptions(CacheOptions());
                var teams = await _inner.GetAllActiveAsync();
                return teams;
            });
    }

    public async Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality)
    {
        if (string.IsNullOrWhiteSpace(assetType) || string.IsNullOrWhiteSpace(criticality)) return null;

        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list))
        {
            var found = list?.FirstOrDefault(t =>
                string.Equals(t.AssetType, assetType.Trim(), StringComparison.OrdinalIgnoreCase)
                && t.TicketCriticality == criticality.Trim());
            if (found != null)
            {
                return found;
            }
        }

        var team = await _inner.GetByAssetTypeAndCriticalityAsync(assetType, criticality);
        if (team != null)
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
        return team;
    }

    public async Task AddAsync(Team team)
    {
        if (team != null)
        {
            await _inner.AddAsync(team);
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());
            _memory.Remove(TeamsListCacheKey);
        }
        else
            throw new ArgumentNullException(nameof(team));
    }

    public async Task UpdateAsync(Team team)
    {
        if (team != null)
        {
            await _inner.UpdateAsync(team);
            _memory.Set(GetIdKey(team.Id), team, CacheOptions());

            // Incremental update: update team in cached list instead of full invalidation
            if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list is List<Team> teamList)
            {
                var index = teamList.FindIndex(t => t.Id == team.Id);
                if (index >= 0)
                {
                    teamList[index] = team;
                }
            }
        }
        else
            throw new ArgumentNullException(nameof(team));
    }

    public async Task RemoveAsync(Team team)
    {
        if (team != null)
        {
            await _inner.RemoveAsync(team);
            _memory.Remove(GetIdKey(team.Id));

            // Incremental update: remove team from cached list instead of full invalidation
            if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list is List<Team> teamList)
            {
                teamList.RemoveAll(t => t.Id == team.Id);
            }
        }
        else
            throw new ArgumentNullException(nameof(team));
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list))
        {
            var exists = list?.Any(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            return exists ?? false;
        }

        return await _inner.ExistsWithNameAsync(name);
    }

    public void RefreshCacheFor(Team team)
    {
        if (team == null) return;
        _memory.Set(GetIdKey(team.Id), team, CacheOptions());

        // Incremental update: update team in cached list instead of full invalidation
        if (_memory.TryGetValue(TeamsListCacheKey, out IEnumerable<Team>? list) && list is List<Team> teamList)
        {
            var index = teamList.FindIndex(t => t.Id == team.Id);
            if (index >= 0)
            {
                teamList[index] = team;
            }
        }
    }

    private static string GetIdKey(Guid id) => $"team_{id:N}";
}
