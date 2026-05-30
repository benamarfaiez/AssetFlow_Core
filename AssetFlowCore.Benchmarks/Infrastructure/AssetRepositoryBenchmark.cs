using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Infrastructure;

/// <summary>
/// Mesure les performances des requêtes EF Core du AssetRepository sans décorateur cache.
/// Permet d'isoler le coût brut EF Core vs le gain apporté par CachedAssetRepository.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class AssetRepositoryBenchmark
{
    private AssetRepository _repository = null!;
    private Guid _knownAssetId;
    private string _existingSerial = null!;

    [Params(10, 100, 500)]
    public int AssetCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseInMemoryDatabase($"Bench_AssetRepo_{AssetCount}")
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                                   .InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AssetFlowDbContext(options);
        _repository = new AssetRepository(db);
        _existingSerial = $"SN-{AssetCount:D6}";

        for (int i = 0; i < AssetCount; i++)
        {
            db.Assets.Add(new Asset(
                i == 0 ? (_knownAssetId = Guid.NewGuid()) : Guid.NewGuid(),
                $"Asset-{i}",
                SerialNumber.Create($"SN-{i:D6}"),
                (AssetType)(i % 3)));
        }

        await db.SaveChangesAsync();
    }

    [Benchmark(Baseline = true, Description = "GetAllReadOnlyAsync — AsNoTracking (N assets)")]
    public async Task<List<Asset>> GetAllReadOnly()
        => (await _repository.GetAllReadOnlyAsync()).ToList();

    [Benchmark(Description = "GetByIdAsync — asset connu avec tickets inclus")]
    public async Task<Asset?> GetById_Found()
        => await _repository.GetByIdAsync(_knownAssetId);

    [Benchmark(Description = "GetByIdAsync — asset inexistant (Guid.NewGuid)")]
    public async Task<Asset?> GetById_NotFound()
        => await _repository.GetByIdAsync(Guid.NewGuid());

    [Benchmark(Description = "ExistsWithSerialNumberAsync — numéro existant")]
    public async Task<bool> ExistsSerial_Found()
        => await _repository.ExistsWithSerialNumberAsync(_existingSerial);

    [Benchmark(Description = "ExistsWithSerialNumberAsync — numéro inexistant")]
    public async Task<bool> ExistsSerial_NotFound()
        => await _repository.ExistsWithSerialNumberAsync("ZZZ-999999");

    [Benchmark(Description = "ExistsWithSerialNumberAsync — avec espaces et casse mixte")]
    public async Task<bool> ExistsSerial_WithTrimAndCase()
        => await _repository.ExistsWithSerialNumberAsync($"  {_existingSerial.ToLower()}  ");
}