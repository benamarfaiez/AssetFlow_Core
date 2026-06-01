using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;
using System.Linq;
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
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 2, iterationCount: 5)]
public class ConcurrentTicketCreationBenchmark : BenchmarkBase
{
    private int _counter;

    [Params(5, 20, 50)]
    public int ConcurrentTickets { get; set; }

    [GlobalSetup]
    public void Setup() => SetupServices($"Bench_Concurrent_{ConcurrentTickets}");

    // Crée N assets frais et retourne leurs IDs
    private async Task<List<Guid>> CreateFreshAssets()
    {
        _counter++;
        var ids = new List<Guid>();
        for (int i = 0; i < ConcurrentTickets; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            DbContext.Assets.Add(new Asset(id, $"Asset-{_counter}-{i}",
                SerialNumber.Create($"CNC-{_counter:D4}-{i:D3}"),
                (AssetType)(i % 3)));
        }
        await DbContext.SaveChangesAsync();
        return ids;
    }

    [Benchmark(Baseline = true, Description = "N tickets séquentiels (référence)")]
    public async Task CreateTickets_Sequential()
    {
        var assetIds = await CreateFreshAssets();
        for (int i = 0; i < ConcurrentTickets; i++)
        {
            var handler = Resolve<CreateMaintenanceTicketHandler>();
            await handler.HandleAsync(new CreateMaintenanceTicketCommand(
                assetIds[i], $"Incident-{i}", "Description", "Medium"));
        }
    }

    [Benchmark(Description = "N tickets en parallèle (Task.WhenAll)")]
    public async Task CreateTickets_Parallel()
    {
        var assetIds = await CreateFreshAssets();
        var tasks = assetIds.Select((id, i) =>
        {
            var handler = Resolve<CreateMaintenanceTicketHandler>();
            return handler.HandleAsync(new CreateMaintenanceTicketCommand(
                id, $"Incident-{i}", "Description", "Medium"));
        });
        await Task.WhenAll(tasks);
    }
}