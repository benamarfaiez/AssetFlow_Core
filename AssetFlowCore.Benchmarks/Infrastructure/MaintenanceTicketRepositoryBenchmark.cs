using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Benchmarks.Infrastructure;

/// <summary>
/// Mesure les performances des requêtes EF Core du MaintenanceTicketRepository.
/// Focus sur CountActiveTicketsByAssetIdAsync — appelé dans DecommissionAsset et CloseTicket.
/// Paramètre : nombre de tickets total pour mesurer la scalabilité de la requête COUNT.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3)]
[RankColumn]
public class MaintenanceTicketRepositoryBenchmark
{
    private MaintenanceTicketRepository _repository = null!;
    private Guid _assetId;
    private readonly Guid _knownTicketId = Guid.NewGuid();

    [Params(5, 20, 50)]
    public int TotalTickets { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseInMemoryDatabase($"Bench_TicketRepo_{TotalTickets}")
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                                   .InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AssetFlowDbContext(options);
        _repository = new MaintenanceTicketRepository(db);
        _assetId = Guid.NewGuid();

        // Répartition réaliste : 70% Opened, 20% InProgress, 10% Closed
        for (int i = 0; i < TotalTickets; i++)
        {
            var ticket = new MaintenanceTicket(
                Guid.NewGuid(), _assetId,
                $"Ticket-{i}", "Description",
                (TicketCriticality)(i % 3),
                Guid.NewGuid()
                );

            if (i % 10 == 0)          // 10% InProgress
            {
                ticket.AssignToTechnician(Guid.NewGuid());
            }
            // Les autres restent Opened

            db.Tickets.Add(ticket);
        }

        // 1 ticket connu pour GetByIdAsync
        var known = new MaintenanceTicket(
            _knownTicketId,
            _assetId,
            "Ticket Connu", "Description connue",
            TicketCriticality.High, Guid.NewGuid());
        db.Tickets.Add(known);

        await db.SaveChangesAsync();
    }

    [Benchmark(Baseline = true, Description = "CountActiveTickets — asset avec N tickets (Opened+InProgress)")]
    public async Task<int> CountActiveTickets()
        => await _repository.CountActiveTicketsByAssetIdAsync(_assetId);

    [Benchmark(Description = "GetByIdAsync — ticket connu (1 résultat)")]
    public async Task<MaintenanceTicket?> GetById_Found()
        => await _repository.GetByIdAsync(_knownTicketId);

    [Benchmark(Description = "GetByIdAsync — ticket inexistant (0 résultat)")]
    public async Task<MaintenanceTicket?> GetById_NotFound()
        => await _repository.GetByIdAsync(Guid.NewGuid());

    [Benchmark(Description = "CountActiveTickets — asset sans tickets (Guid inconnu)")]
    public async Task<int> CountActiveTickets_NoTickets()
        => await _repository.CountActiveTicketsByAssetIdAsync(Guid.NewGuid());
}