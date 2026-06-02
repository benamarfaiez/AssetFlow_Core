using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Enums;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Application;

/// <summary>
/// Mesure la performance du moteur de résolution de stratégies (Pattern Strategy).
/// Cas critique : appelé à chaque création de ticket — doit être quasi-gratuit.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3)]
[RankColumn]
public class TicketAssignmentEngineBenchmark
{
    private ITicketAssignmentEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        var strategies = new List<IAssignmentStrategy>
        {
            new ServerAssignmentStrategy(),
            new NetworkAssignmentStrategy(),
            new LaptopHighCriticalityStrategy(),
            new LaptopStandardStrategy()
        };
        _engine = new TicketAssignmentEngine(strategies);
    }

    [Benchmark(Baseline = true, Description = "Server → Infrastructure-Serveurs")]
    public string ResolveServer()
        => _engine.ResolveTeam(AssetType.Server, TicketCriticality.High);

    [Benchmark(Description = "Laptop High → Support-VIP")]
    public string ResolveLaptopHighCriticality()
        => _engine.ResolveTeam(AssetType.Laptop, TicketCriticality.High);

    [Benchmark(Description = "Laptop Medium → Support-Lectorat")]
    public string ResolveLaptopStandard()
        => _engine.ResolveTeam(AssetType.Laptop, TicketCriticality.Medium);

    [Benchmark(Description = "Network → Réseau-Télécom")]
    public string ResolveNetworkDevice()
        => _engine.ResolveTeam(AssetType.NetworkDevice, TicketCriticality.Low);

    [Benchmark(Description = "Type inconnu → Support-Général (fallback)")]
    public string ResolveFallback()
        => _engine.ResolveTeam((AssetType)99, TicketCriticality.Low);
}