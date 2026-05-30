using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure le coût de création de N tickets sur des assets distincts en parallèle.
/// Simule une charge réelle où plusieurs techniciens déclarent des incidents simultanément.
/// Mesure le throughput du pipeline complet sous charge concurrente.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ConcurrentTicketCreationBenchmark : BenchmarkBase
{
    private List<Guid> _assetIds = new();

    [Params(5, 20, 50)]
    public int ConcurrentTickets { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices($"Bench_Concurrent_{ConcurrentTickets}");

        // Crée N assets distincts (1 par ticket concurrent — évite la contrainte MarkAsDown)
        for (int i = 0; i < ConcurrentTickets; i++)
        {
            var id = Guid.NewGuid();
            _assetIds.Add(id);
            DbContext.Assets.Add(new Asset(id, $"Asset-{i}",
                SerialNumber.Create($"CNC-{i:D5}"), (AssetType)(i % 3)));
        }
        await DbContext.SaveChangesAsync();
    }

    [IterationSetup]
    public void RestoreAllAssets()
    {
        foreach (var asset in DbContext.Assets)
            asset.RestoreToService();
        DbContext.SaveChanges();
    }

    [Benchmark(Baseline = true, Description = "N tickets séquentiels (référence)")]
    public async Task CreateTickets_Sequential()
    {
        for (int i = 0; i < ConcurrentTickets; i++)
        {
            var handler = Resolve<CreateMaintenanceTicketHandler>();
            await handler.HandleAsync(new CreateMaintenanceTicketCommand(
                _assetIds[i], $"Incident-{i}", "Description", "Medium"));
        }
    }

    [Benchmark(Description = "N tickets en parallèle (Task.WhenAll)")]
    public async Task CreateTickets_Parallel()
    {
        var tasks = _assetIds.Select((id, i) =>
        {
            var handler = Resolve<CreateMaintenanceTicketHandler>();
            return handler.HandleAsync(new CreateMaintenanceTicketCommand(
                id, $"Incident-{i}", "Description", "Medium"));
        });
        await Task.WhenAll(tasks);
    }
}