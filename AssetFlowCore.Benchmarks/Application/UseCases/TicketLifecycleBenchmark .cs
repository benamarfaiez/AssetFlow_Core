using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure le cycle de vie complet d'un ticket :
/// Create → Assign → Close
/// Permet de mesurer le coût de l'automate d'état en cascade sur l'asset lié.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TicketLifecycleBenchmark : BenchmarkBase
{
    private Guid _assetId;
    private int _counter;

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices("Bench_TicketLifecycle");
        _assetId = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(
            _assetId, "Asset-Lifecycle",
            SerialNumber.Create("LCY-BENCH-01"),
            AssetType.Laptop));
        await DbContext.SaveChangesAsync();
    }

    [IterationSetup]
    public void RestoreAsset()
    {
        foreach (var a in DbContext.Assets) a.RestoreToService();
        DbContext.SaveChanges();
        _counter++;
    }

    /// <summary>
    /// Benchmark le pipeline complet Create → Assign → Close en une seule mesure.
    /// Reflète le scénario réel d'un incident résolu de bout en bout.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Cycle complet : Create → Assign → Close")]
    public async Task FullLifecycle()
    {
        // 1. Création du ticket
        var createHandler = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await createHandler.HandleAsync(new CreateMaintenanceTicketCommand(
            _assetId, $"Incident {_counter}", "Description", "Medium"));

        // 2. Prise en charge par un technicien
        var assignHandler = Resolve<AssignTicketToTechnicianHandler>();
        await assignHandler.ExecuteAsync(new AssignTicketToTechnicianCommand(ticket.Id));

        // 3. Clôture avec commentaire de résolution
        var closeHandler = Resolve<CloseTicketHandler>();
        await closeHandler.ExecuteAsync(new CloseTicketCommand(ticket.Id, "Problème résolu."));
    }

    [Benchmark(Description = "Assign seul (ticket déjà créé)")]
    public async Task AssignOnly()
    {
        var createHandler = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await createHandler.HandleAsync(new CreateMaintenanceTicketCommand(
            _assetId, $"Incident-Assign {_counter}", "Description", "High"));

        var assignHandler = Resolve<AssignTicketToTechnicianHandler>();
        await assignHandler.ExecuteAsync(new AssignTicketToTechnicianCommand(ticket.Id));
    }

    [Benchmark(Description = "Close seul (ticket déjà assigné)")]
    public async Task CloseOnly()
    {
        var createHandler = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await createHandler.HandleAsync(new CreateMaintenanceTicketCommand(
            _assetId, $"Incident-Close {_counter}", "Description", "Low"));

        var assignHandler = Resolve<AssignTicketToTechnicianHandler>();
        await assignHandler.ExecuteAsync(new AssignTicketToTechnicianCommand(ticket.Id));

        var closeHandler = Resolve<CloseTicketHandler>();
        await closeHandler.ExecuteAsync(new CloseTicketCommand(ticket.Id, "Clôturé."));
    }
}