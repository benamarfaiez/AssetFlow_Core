using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Domain;

/// <summary>
/// Mesure le coût des transitions d'état de l'entité MaintenanceTicket.
/// Automate : Opened → InProgress → Closed
/// Inclut aussi le coût de construction avec validation des invariants.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3)]
[RankColumn]
public class MaintenanceTicketStateMachineBenchmark
{
    private Guid _assetId;

    [GlobalSetup]
    public void Setup() => _assetId = Guid.NewGuid();

    [Benchmark(Baseline = true, Description = "Ticket construction — validation invariants")]
    public MaintenanceTicket Construction()
        => new(Guid.NewGuid(), _assetId, "Panne réseau", "Description", TicketCriticality.High, Guid.NewGuid());

    [Benchmark(Description = "Ticket.AssignToTechnician() — Opened → InProgress")]
    public void AssignToTechnician()
    {
        var t = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre-1", "Desc", TicketCriticality.Medium, Guid.NewGuid());
        t.AssignToTechnician();
    }

    [Benchmark(Description = "Ticket.Close() — InProgress → Closed")]
    public void Close()
    {
        var t = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre-2", "Desc", TicketCriticality.Low, Guid.NewGuid());
        t.AssignToTechnician();
        t.Close("Problème résolu.");
    }

    [Benchmark(Description = "Cycle complet : Construction → Assign → Close")]
    public MaintenanceTicket FullCycle()
    {
        var t = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Incident complet", "Desc", TicketCriticality.High, Guid.NewGuid());
        t.AssignToTechnician();
        t.Close("Résolu après remplacement disque.");
        return t;
    }

    [Benchmark(Description = "Ticket — criticality Low vs High construction cost")]
    public (MaintenanceTicket low, MaintenanceTicket high) CriticalityVariants()
    {
        var low = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre-3", "Desc", TicketCriticality.Low, Guid.NewGuid());
        var high = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre-4", "Desc", TicketCriticality.High, Guid.NewGuid());
        return (low, high);
    }
}