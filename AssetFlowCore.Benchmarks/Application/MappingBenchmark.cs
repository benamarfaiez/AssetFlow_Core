using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Application;

/// <summary>
/// Mesure le coût du mapping manuel (MappingExtensions.ToDto).
/// L'application utilise intentionnellement du mapping manuel au lieu
/// d'AutoMapper — ce benchmark valide que ce choix est effectivement
/// plus performant et n'alloue pas de mémoire inutilement.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MappingBenchmark
{
    private Asset _asset = null!;
    private MaintenanceTicket _ticket = null!;
    private List<Asset> _assetList = null!;

    [Params(1, 50, 200)]
    public int ListSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var assetId = Guid.NewGuid();
        _asset = new Asset(assetId, "Serveur-Mapping", SerialNumber.Create("MAP-SRV-01"), AssetType.Server);
        _ticket = new MaintenanceTicket(Guid.NewGuid(), assetId, "Test Mapping", "Description", TicketCriticality.High, "Infrastructure-Serveurs");

        _assetList = Enumerable.Range(0, ListSize)
            .Select(i => new Asset(Guid.NewGuid(), $"Asset-{i}", SerialNumber.Create($"SN-{i:D5}"),
                (AssetType)(i % 3)))
            .ToList();
    }

    [Benchmark(Baseline = true, Description = "Asset.ToDto() — mapping unitaire")]
    public AssetResponseDto MapSingleAsset() => _asset.ToDto();

    [Benchmark(Description = "Ticket.ToDto() — mapping unitaire")]
    public TicketResponseDto MapSingleTicket() => _ticket.ToDto();

    [Benchmark(Description = "Liste assets.Select(ToDto) — mapping en masse")]
    public List<AssetResponseDto> MapAssetList()
        => _assetList.Select(a => a.ToDto()).ToList();
}