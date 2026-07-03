using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Domain;

/// <summary>
/// Mesure le coût des transitions de l'automate d'état de l'entité Asset.
/// Chaque méthode représente une transition valide de la machine d'états :
/// InService → Down → InMaintenance → InService / Decommissioned
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3)]
[RankColumn]
public class AssetStateMachineBenchmark
{
    [Benchmark(Baseline = true, Description = "Asset.MarkAsDown() — InService → Down")]
    public static void MarkAsDown()
    {
        var asset = new Asset(Guid.NewGuid(), "Serveur-1", SerialNumber.Create("SRV-STM-01"), AssetType.Server);
        asset.MarkAsDown();
    }

    [Benchmark(Description = "Asset.MarkInMaintenance() — Down → InMaintenance")]
    public static void MarkInMaintenance()
    {
        var asset = new Asset(Guid.NewGuid(), "Serveur-2", SerialNumber.Create("SRV-STM-02"), AssetType.Server);
        asset.MarkAsDown();
        asset.MarkInMaintenance();
    }

    [Benchmark(Description = "Asset.RestoreToService() — InMaintenance → InService")]
    public static void RestoreToService()
    {
        var asset = new Asset(Guid.NewGuid(), "Serveur-3", SerialNumber.Create("SRV-STM-03"), AssetType.Server);
        asset.MarkAsDown();
        asset.MarkInMaintenance();
        asset.RestoreToService();
    }

    [Benchmark(Description = "Asset.Decommission() — InService → Decommissioned")]
    public static void Decommission()
    {
        var asset = new Asset(Guid.NewGuid(), "Serveur-4", SerialNumber.Create("SRV-STM-04"), AssetType.Server);
        asset.Decommission();
    }

    [Benchmark(Description = "Cycle complet : InService → Down → InMaintenance → InService")]
    public static void FullStateCycle()
    {
        var asset = new Asset(Guid.NewGuid(), "Serveur-5", SerialNumber.Create("SRV-STM-05"), AssetType.Server);
        asset.MarkAsDown();
        asset.MarkInMaintenance();
        asset.RestoreToService();
    }

    [Benchmark(Description = "Asset() construction seule — coût d'instanciation")]
    public static Asset AssetConstruction()
        => new(Guid.NewGuid(), "Serveur-Perf", SerialNumber.Create("SRV-STM-06"), AssetType.Server);
}