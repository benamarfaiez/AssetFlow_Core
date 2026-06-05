using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure l'impact du pattern Décorateur Cache (CachedAssetRepository) sur GetAllAssets.
/// Compare : premier appel (cache miss → SQL) vs appels suivants (cache hit → mémoire).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 3)]
[RankColumn]
public class GetAllAssetsBenchmark : BenchmarkBase
{
    [Params(5, 20, 50)]
    public int AssetCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices($"Bench_GetAll_{AssetCount}");

        // Pré-populate la base avec N assets
        for (int i = 0; i < AssetCount; i++)
        {
            var assetType = (i % 3) switch
            {
                0 => AssetType.Server,
                1 => AssetType.Laptop,
                2 => AssetType.NetworkDevice,
                _ => throw new NotImplementedException()
            };

            DbContext.Assets.Add(new Asset(
                Guid.NewGuid(),
                $"Asset-{i}",
                SerialNumber.Create($"SN-{i:D6}"),
                assetType));
        }
        await DbContext.SaveChangesAsync();
    }

    [Benchmark(Baseline = true, Description = "GetAll — cache miss (premier appel)")]
    public async Task<List<AssetResponseDto>> GetAll_CacheMiss()
    {
        var handler = Resolve<GetAllAssetsHandler>();
        return [.. (await handler.HandleAsync(new GetAllAssetsQuery()))];
    }

    [Benchmark(Description = "GetAll — cache hit (appels suivants)")]
    public async Task<List<AssetResponseDto>> GetAll_CacheHit()
    {
        var handler = Resolve<GetAllAssetsHandler>();
        await handler.HandleAsync(new GetAllAssetsQuery()); // chauffe
        return [.. (await handler.HandleAsync(new GetAllAssetsQuery()))];
    }
}