using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure le cycle de vie complet d'un ticket :
/// Create → Assign → Close
/// Permet de mesurer le coût de l'automate d'état en cascade sur l'asset lié.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 5)]
public class TicketLifecycleBenchmark : BenchmarkBase
{
    private int _counter;

    [GlobalSetup]
    public void Setup() => SetupServices("Bench_TicketLifecycle");

    // Crée un asset frais par itération — pas de conflit d'état entre benchmarks
    private async Task<Guid> CreateFreshAsset()
    {
        _counter++;
        var id = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(id, $"Asset-LC-{_counter}",
            SerialNumber.Create($"LCY-{_counter:D6}"), AssetType.Laptop));
        await DbContext.SaveChangesAsync();
        return id;
    }

    [Benchmark(Baseline = true, Description = "Cycle complet : Create → Assign → Close")]
    public async Task FullLifecycle()
    {
        var assetId = await CreateFreshAsset();

        var createHandler = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await createHandler.Handle(new CreateMaintenanceTicketCommand(
            assetId, $"Incident {_counter}", "Description", "Medium"), CancellationToken.None);

        var assignHandler = Resolve<AssignTicketToTechnicianHandler>();
        await assignHandler.Handle(new AssignTicketToTechnicianCommand(ticket.Id), CancellationToken.None);

        var closeHandler = Resolve<CloseTicketHandler>();
        await closeHandler.Handle(new CloseTicketCommand(ticket.Id, "Problème résolu."), CancellationToken.None);
    }

    [Benchmark(Description = "Assign seul (sans Close)")]
    public async Task AssignOnly()
    {
        var assetId = await CreateFreshAsset();

        var createHandler = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await createHandler.Handle(new CreateMaintenanceTicketCommand(
            assetId, $"Incident-Assign {_counter}", "Description", "High"), CancellationToken.None);

        var assignHandler = Resolve<AssignTicketToTechnicianHandler>();
        await assignHandler.Handle(new AssignTicketToTechnicianCommand(ticket.Id), CancellationToken.None);
    }

    [Benchmark(Description = "Close seul (après Create + Assign)")]
    public async Task CloseOnly()
    {
        var assetId = await CreateFreshAsset();

        var createHandler = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await createHandler.Handle(new CreateMaintenanceTicketCommand(
            assetId, $"Incident-Close {_counter}", "Description", "Low"), CancellationToken.None);

        var assignHandler = Resolve<AssignTicketToTechnicianHandler>();
        await assignHandler.Handle(new AssignTicketToTechnicianCommand(ticket.Id), CancellationToken.None);

        var closeHandler = Resolve<CloseTicketHandler>();
        await closeHandler.Handle(new CloseTicketCommand(ticket.Id, "Clôturé."), CancellationToken.None);
    }
}