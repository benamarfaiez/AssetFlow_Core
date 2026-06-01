using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure les deux chemins du cas d'utilisation DecommissionAsset :
/// 1. Happy path : asset sans tickets actifs → décommissionnement réussi.
/// 2. Blocked path : asset avec N tickets actifs → DomainException (règle métier bloquante).
/// Le chemin bloqué est important car CountActiveTicketsByAssetIdAsync est une requête SQL supplémentaire.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 10)]
public class DecommissionAssetBenchmark : BenchmarkBase
{
    private int _counter;

    [Params(1, 5, 10)]
    public int ActiveTicketCount { get; set; }

    [GlobalSetup]
    public void Setup() => SetupServices($"Bench_Decommission_{ActiveTicketCount}");

    // Crée un asset propre (sans tickets) — chemin succès
    private async Task<Guid> CreateCleanAsset()
    {
        _counter++;
        var id = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(id, $"Clean-{_counter}",
            SerialNumber.Create($"CLN-{_counter:D6}"), AssetType.Server));
        await DbContext.SaveChangesAsync();
        return id;
    }

    // Crée un asset avec N tickets actifs — chemin bloqué
    private async Task<Guid> CreateBlockedAsset()
    {
        _counter++;
        var id = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(id, $"Blocked-{_counter}",
            SerialNumber.Create($"BLK-{_counter:D6}"), AssetType.Server));
        await DbContext.SaveChangesAsync();

        for (int i = 0; i < ActiveTicketCount; i++)
        {
            // Remet l'asset en InService avant chaque ticket
            var asset = await DbContext.Assets.FindAsync(id);
            asset!.RestoreToService();
            await DbContext.SaveChangesAsync();

            var handler = Resolve<CreateMaintenanceTicketHandler>();
            await handler.HandleAsync(new CreateMaintenanceTicketCommand(
                id, $"Ticket-{i}", "Description", "Low"));
        }
        return id;
    }

    [Benchmark(Baseline = true, Description = "Decommission — asset sans tickets (succès)")]
    public async Task Decommission_Success()
    {
        var assetId = await CreateCleanAsset();
        var handler = Resolve<DecommissionAssetHandler>();
        await handler.ExecuteAsync(new DecommissionAssetCommand(assetId));
    }

    [Benchmark(Description = "Decommission — asset bloqué par tickets actifs (DomainException)")]
    public async Task Decommission_Blocked()
    {
        var assetId = await CreateBlockedAsset();
        var handler = Resolve<DecommissionAssetHandler>();
        try
        {
            await handler.ExecuteAsync(new DecommissionAssetCommand(assetId));
        }
        catch (DomainException)
        {
            // Attendu — on mesure le coût du chemin bloquant
        }
    }
}