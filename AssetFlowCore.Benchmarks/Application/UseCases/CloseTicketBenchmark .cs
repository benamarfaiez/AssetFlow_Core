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
/// Mesure le coût de CloseTicket selon le nombre de tickets actifs restants.
/// L'automate d'état en cascade (RestoreToService) ne se déclenche que
/// si remainingActiveTickets <= 1 — ce benchmark valide que le COUNT SQL
/// supplémentaire ne dégrade pas les performances.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 2, iterationCount: 3, invocationCount: 10)]
public class CloseTicketBenchmark : BenchmarkBase
{
    private int _counter;

    [GlobalSetup]
    public void Setup() => SetupServices("Bench_CloseTicket");

    [Benchmark(Baseline = true, Description = "Close — dernier ticket actif → RestoreToService")]
    public async Task Close_LastTicket_TriggersRestore()
    {
        _counter++;

        var assetId = Guid.NewGuid();
        var asset = new Asset(assetId, $"Asset-Close-{_counter}", SerialNumber.Create($"CLO-{_counter:D6}"), AssetType.Server);
        // Met l'actif en panne pour permettre l'entrée en maintenance lors de l'assignation du ticket
        asset.MarkAsDown();

        await DbContext.Assets.AddAsync(asset);
        await DbContext.SaveChangesAsync();

        // Crée le ticket
        var create = Resolve<CreateMaintenanceTicketHandler>();
        var ticket = await create.HandleAsync(new CreateMaintenanceTicketCommand(assetId, $"Ticket-{_counter}", "Description", "Medium"));

        // Assigne le ticket
        var assign = Resolve<AssignTicketToTechnicianHandler>();
        await assign.ExecuteAsync(new AssignTicketToTechnicianCommand(ticket.Id));

        // Ferme le ticket — déclenche RestoreToService (dernier ticket actif)
        var close = Resolve<CloseTicketHandler>();
        await close.ExecuteAsync(new CloseTicketCommand(ticket.Id, "Résolu."));
    }
}