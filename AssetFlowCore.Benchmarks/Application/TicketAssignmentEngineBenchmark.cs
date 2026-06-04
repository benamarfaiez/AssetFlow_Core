using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Moq;

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
    private const ITicketAssignmentEngine value = null!;
    private ITicketAssignmentEngine _engine = value;
    private ITeamRepository? _teamRepository;

    [GlobalSetup]
    public void Setup()
    {
        var repoMock = new Mock<ITeamRepository>();
        repoMock
            .Setup(r => r.GetByAssetTypeAndCriticalityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Team("Infra-Serveurs", "Server", "High", "Description"));

        _teamRepository = repoMock.Object;

        var strategies = new List<IAssignmentStrategy>
        {
            new ServerAssignmentStrategy(_teamRepository),
            new NetworkAssignmentStrategy(_teamRepository),
            new LaptopHighCriticalityStrategy(_teamRepository),
            new LaptopStandardStrategy(_teamRepository)
        };
        _engine = new TicketAssignmentEngine(strategies);
    }

    [Benchmark(Baseline = true, Description = "Server → Infrastructure-Serveurs")]
    public async Task<string> ResolveServer()
        => await _engine.ResolveTeamIdAsync(AssetType.Server, TicketCriticality.High);

    [Benchmark(Description = "Laptop High → Support-VIP")]
    public async Task<string> ResolveLaptopHighCriticality()
        => await _engine.ResolveTeamIdAsync(AssetType.Laptop, TicketCriticality.High);

    [Benchmark(Description = "Laptop Medium → Support-Lectorat")]
    public async Task<string> ResolveLaptopStandard()
        => await _engine.ResolveTeamIdAsync(AssetType.Laptop, TicketCriticality.Medium);

    [Benchmark(Description = "Network → Réseau-Télécom")]
    public async Task<string> ResolveNetworkDevice()
        => await _engine.ResolveTeamIdAsync(AssetType.NetworkDevice, TicketCriticality.Low);

    [Benchmark(Description = "Type inconnu → Support-Général (fallback)")]
    public async Task<string> ResolveFallback()
        => await _engine.ResolveTeamIdAsync((AssetType)99, TicketCriticality.Low);
}