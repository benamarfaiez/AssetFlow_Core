using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure l'impact du pattern Décorateur Cache (CachedAssetRepository) sur GetAllAssets.
/// Compare : premier appel (cache miss → SQL) vs appels suivants (cache hit → mémoire).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class GetAllAssetsBenchmark : BenchmarkBase
{
    [Params(10, 100, 500)]
    public int AssetCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices($"Bench_GetAll_{AssetCount}");

        // Pré-populate la base avec N assets
        for (int i = 0; i < AssetCount; i++)
        {
            DbContext.Assets.Add(new Asset(
                Guid.NewGuid(),
                $"Asset-{i}",
                SerialNumber.Create($"SN-{i:D6}"),
                i % 3 == 0 ? AssetType.Server
              : i % 3 == 1 ? AssetType.Laptop
                            : AssetType.NetworkDevice));
        }
        await DbContext.SaveChangesAsync();
    }

    [Benchmark(Baseline = true, Description = "GetAll — cache miss (premier appel)")]
    public async Task<List<AssetResponseDto>> GetAll_CacheMiss()
    {
        var handler = Resolve<GetAllAssetsHandler>();
        return (await handler.HandleAsync(new GetAllAssetsQuery())).ToList();
    }

    [Benchmark(Description = "GetAll — cache hit (appels suivants)")]
    public async Task<List<AssetResponseDto>> GetAll_CacheHit()
    {
        var handler = Resolve<GetAllAssetsHandler>();
        await handler.HandleAsync(new GetAllAssetsQuery()); // chauffe
        return (await handler.HandleAsync(new GetAllAssetsQuery())).ToList();
    }
}