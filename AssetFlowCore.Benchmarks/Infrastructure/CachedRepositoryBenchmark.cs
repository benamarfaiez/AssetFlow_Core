using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Benchmarks.Infrastructure;

/// <summary>
/// Mesure directement le pattern Décorateur Cache (CachedAssetRepository).
/// Compare les performances brutes sans le décorateur vs avec le décorateur.
/// Valide le gain du cache sur GetAllReadOnlyAsync.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3)]
[RankColumn]
public class CachedRepositoryBenchmark
{
    private AssetRepository _rawRepository = null!;
    private CachedAssetRepository _cachedRepository = null!;
    private IMemoryCache _cache = null!;

    [Params(5, 20, 50)]
    public int AssetCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseInMemoryDatabase($"Bench_Cache_{AssetCount}")
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                                   .InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var dbContext = new AssetFlowDbContext(options);

        for (int i = 0; i < AssetCount; i++)
        {
            dbContext.Assets.Add(new Asset(
                Guid.NewGuid(), $"Asset-{i}",
                SerialNumber.Create($"SN-{i:D6}"),
                (AssetType)(i % 3)));
        }
        await dbContext.SaveChangesAsync();

        _rawRepository = new AssetRepository(dbContext);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _cachedRepository = new CachedAssetRepository(_rawRepository, _cache);
    }

    [Benchmark(Baseline = true, Description = "Sans cache — lecture EF directe")]
    public async Task<List<Asset>> GetAll_NoCache()
        => (await _rawRepository.GetAllReadOnlyAsync()).ToList();

    [Benchmark(Description = "Cache miss — premier appel (populate)")]
    public async Task<List<Asset>> GetAll_CacheMiss()
    {
        _cache.Remove("Assets_List_ReadOnly"); // Vide le cache pour forcer un miss
        return (await _cachedRepository.GetAllReadOnlyAsync()).ToList();
    }

    [Benchmark(Description = "Cache hit — appels suivants")]
    public async Task<List<Asset>> GetAll_CacheHit()
    {
        await _cachedRepository.GetAllReadOnlyAsync(); // Chauffe le cache
        return (await _cachedRepository.GetAllReadOnlyAsync()).ToList();
    }
}