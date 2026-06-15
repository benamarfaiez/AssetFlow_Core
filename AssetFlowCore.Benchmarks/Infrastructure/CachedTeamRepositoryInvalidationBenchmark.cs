using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VSDiagnostics;

namespace AssetFlowCore.Benchmarks.Infrastructure;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 2, iterationCount: 5)]
[RankColumn]
[CPUUsageDiagnoser]
public class CachedTeamRepositoryInvalidationBenchmark
{
    private TeamRepository _innerRepository = null!;
    private CachedTeamRepository _cachedRepository = null!;
    private MemoryCache _cache = null!;
    private AssetFlowDbContext _dbContext = null!;
    private List<Guid> _teamIds = null!;
    [Params(10, 50, 100)]
    public int TeamCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>().UseInMemoryDatabase($"Bench_CachedTeam_{TeamCount}_{Guid.NewGuid():N}").ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
        _dbContext = new AssetFlowDbContext(options);
        _teamIds = [];
        for (int i = 0; i < TeamCount; i++)
        {
            var assetType = (i % 3) switch
            {
                0 => AssetType.Server,
                1 => AssetType.Laptop,
                _ => AssetType.NetworkDevice
            };
            var criticality = (i % 3) switch
            {
                0 => TicketCriticality.High,
                1 => TicketCriticality.Medium,
                _ => TicketCriticality.Low
            };
            var team = new Team($"Team-{i}", assetType.ToString(), criticality.ToString(), $"Description-{i}");
            _dbContext.Teams.Add(team);
            _teamIds.Add(team.Id);
        }

        await _dbContext.SaveChangesAsync();
        _innerRepository = new TeamRepository(_dbContext);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _cachedRepository = new CachedTeamRepository(_innerRepository, _cache);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cache?.Dispose();
        _dbContext?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Current: Full list invalidation on each update")]
    public async Task Mixed_ReadWriteWithFullInvalidation()
    {
        // Warm cache with initial GetAllActiveAsync
        await _cachedRepository.GetAllActiveAsync();
        // Simulate mixed workload: 90% reads, 10% writes (100 iterations)
        for (int i = 0; i < 100; i++)
        {
            if (i % 10 == 0)
            {
                // 10% Update operation (triggers full list cache invalidation)
                var teamId = _teamIds[i % _teamIds.Count];
                var team = await _dbContext.Teams.FindAsync(teamId);
                if (team != null)
                {
                    team.UpdateDescription($"Updated at iteration {i}");
                    await _cachedRepository.UpdateAsync(team);
                }
            }
            else
            {
                // 90% Read operations
                await _cachedRepository.GetAllActiveAsync();
            }
        }
    }

    [Benchmark(Description = "Baseline: No cache (direct DB for all ops)")]
    public async Task Mixed_ReadWriteNoCache()
    {
        // Same workload but bypass cache entirely
        for (int i = 0; i < 100; i++)
        {
            if (i % 10 == 0)
            {
                var teamId = _teamIds[i % _teamIds.Count];
                var team = await _dbContext.Teams.FindAsync(teamId);
                if (team != null)
                {
                    team.UpdateDescription($"Updated at iteration {i}");
                    await _innerRepository.UpdateAsync(team);
                }
            }
            else
            {
                await _innerRepository.GetAllActiveAsync();
            }
        }
    }

    [Benchmark(Description = "Cache invalidation cascade: repeated update/read cycles")]
    public async Task CacheInvalidationCascade()
    {
        // Simulate scenario where cache is invalidated by update, forcing subsequent reads to reload
        var teamId = _teamIds[0];
        // Warm cache
        await _cachedRepository.GetAllActiveAsync();
        // Perform 20 update/read cycles
        for (int i = 0; i < 20; i++)
        {
            var team = await _dbContext.Teams.FindAsync(teamId);
            if (team != null)
            {
                team.UpdateDescription($"Cycle {i}");
                await _cachedRepository.UpdateAsync(team); // ← Full list cache invalidation
                // Subsequent reads must reload from DB (cache miss)
                await _cachedRepository.GetAllActiveAsync();
            }
        }
    }
}