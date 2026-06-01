using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Domain;

/// <summary>
/// Mesure le coût des transitions d'état de l'entité MaintenanceTicket.
/// Automate : Opened → InProgress → Closed
/// Inclut aussi le coût de construction avec validation des invariants.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MaintenanceTicketStateMachineBenchmark
{
    private Guid _assetId;

    [GlobalSetup]
    public void Setup() => _assetId = Guid.NewGuid();

    [Benchmark(Baseline = true, Description = "Ticket construction — validation invariants")]
    public MaintenanceTicket Construction()
        => new MaintenanceTicket(Guid.NewGuid(), _assetId, "Panne réseau", "Description", TicketCriticality.High, "Réseau-Télécom");

    [Benchmark(Description = "Ticket.AssignToTechnician() — Opened → InProgress")]
    public void AssignToTechnician()
    {
        var t = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre", "Desc", TicketCriticality.Medium, "Support-VIP");
        t.AssignToTechnician();
    }

    [Benchmark(Description = "Ticket.Close() — InProgress → Closed")]
    public void Close()
    {
        var t = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre", "Desc", TicketCriticality.Low, "Support-Lectorat");
        t.AssignToTechnician();
        t.Close("Problème résolu.");
    }

    [Benchmark(Description = "Cycle complet : Construction → Assign → Close")]
    public MaintenanceTicket FullCycle()
    {
        var t = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Incident complet", "Desc", TicketCriticality.High, "Infrastructure-Serveurs");
        t.AssignToTechnician();
        t.Close("Résolu après remplacement disque.");
        return t;
    }

    [Benchmark(Description = "Ticket — criticality Low vs High construction cost")]
    public (MaintenanceTicket low, MaintenanceTicket high) CriticalityVariants()
    {
        var low = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre", "Desc", TicketCriticality.Low, "Team-A");
        var high = new MaintenanceTicket(Guid.NewGuid(), _assetId, "Titre", "Desc", TicketCriticality.High, "Team-B");
        return (low, high);
    }
}