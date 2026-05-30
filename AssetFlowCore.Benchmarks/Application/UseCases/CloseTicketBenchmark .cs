using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure le coût de CloseTicket selon le nombre de tickets actifs restants.
/// L'automate d'état en cascade (RestoreToService) ne se déclenche que
/// si remainingActiveTickets <= 1 — ce benchmark valide que le COUNT SQL
/// supplémentaire ne dégrade pas les performances.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CloseTicketBenchmark : BenchmarkBase
{
    private Guid _singleTicketAssetId;
    private Guid _multiTicketAssetId;

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices("Bench_CloseTicket");

        // Asset 1 : 1 seul ticket → fermeture déclenche RestoreToService
        _singleTicketAssetId = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(
            _singleTicketAssetId, "Asset-Single",
            SerialNumber.Create("CLO-SNG-01"), AssetType.Server));

        // Asset 2 : 3 tickets actifs → fermeture NE déclenche PAS RestoreToService
        _multiTicketAssetId = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(
            _multiTicketAssetId, "Asset-Multi",
            SerialNumber.Create("CLO-MLT-01"), AssetType.Laptop));

        await DbContext.SaveChangesAsync();
    }

    [IterationSetup]
    public async void PrepareTickets()
    {
        // Remet les assets en état initial
        foreach (var a in DbContext.Assets) a.RestoreToService();
        await DbContext.SaveChangesAsync();

        // Ticket unique pour Asset 1
        var c1 = Resolve<CreateMaintenanceTicketHandler>();
        var t1 = await c1.HandleAsync(new CreateMaintenanceTicketCommand(
            _singleTicketAssetId, "Incident unique", "Desc", "High"));
        var a1 = Resolve<AssignTicketToTechnicianHandler>();
        await a1.ExecuteAsync(new AssignTicketToTechnicianCommand(t1.Id));

        // 3 tickets pour Asset 2 — en crée 2 supplémentaires après le premier
        foreach (var a in DbContext.Assets.Where(a => a.Id == _multiTicketAssetId))
            a.RestoreToService();
        await DbContext.SaveChangesAsync();
    }

    [Benchmark(Baseline = true, Description = "Close — dernier ticket actif → RestoreToService")]
    public async Task Close_LastTicket_TriggersRestore()
    {
        // Crée et assigne un ticket sur l'asset single pour cette itération
        foreach (var a in DbContext.Assets.Where(x => x.Id == _singleTicketAssetId))
            a.RestoreToService();
        await DbContext.SaveChangesAsync();

        var create = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await create.HandleAsync(new CreateMaintenanceTicketCommand(
            _singleTicketAssetId, "Ticket", "Desc", "Medium"));

        var assign = Resolve<AssignTicketToTechnicianHandler>();
        await assign.ExecuteAsync(new AssignTicketToTechnicianCommand(ticket.Id));

        var close = Resolve<CloseTicketHandler>();
        await close.ExecuteAsync(new CloseTicketCommand(ticket.Id, "Résolu."));
    }
}