using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
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
public class DecommissionAssetBenchmark : BenchmarkBase
{
    private Guid _cleanAssetId;
    private Guid _blockedAssetId;
    private int _counter;

    [Params(1, 5, 10)]
    public int ActiveTicketCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices($"Bench_Decommission_{ActiveTicketCount}");

        // Asset sans ticket — décommissionnement possible
        _cleanAssetId = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(
            _cleanAssetId, "Asset-Clean",
            SerialNumber.Create("DEC-CLEAN-01"), AssetType.Server));

        // Asset avec N tickets actifs — bloqué
        _blockedAssetId = Guid.NewGuid();
        var blockedAsset = new Asset(
            _blockedAssetId, "Asset-Bloqué",
            SerialNumber.Create("DEC-BLOCK-01"), AssetType.Server);
        DbContext.Assets.Add(blockedAsset);

        await DbContext.SaveChangesAsync();

        // Crée N tickets actifs sur l'asset bloqué
        for (int i = 0; i < ActiveTicketCount; i++)
        {
            blockedAsset.RestoreToService(); // reset pour permettre MarkAsDown
            var createHandler = Resolve<CreateMaintenanceTicketHandler>();
            await createHandler.HandleAsync(new CreateMaintenanceTicketCommand(
                _blockedAssetId, $"Ticket-{i}", "Description", "Low"));
        }
    }

    [IterationSetup]
    public void RestoreCleanAsset()
    {
        // Remet l'asset clean en InService pour le prochain benchmark
        var asset = DbContext.Assets.Find(_cleanAssetId);
        asset?.RestoreToService();
        DbContext.SaveChanges();
        _counter++;
    }

    [Benchmark(Baseline = true, Description = "Decommission — asset sans tickets (succès)")]
    public async Task Decommission_Success()
    {
        // Crée un nouvel asset propre pour chaque itération
        var freshId = Guid.NewGuid();
        DbContext.Assets.Add(new Asset(freshId, $"Fresh-{_counter}", SerialNumber.Create($"FRH-{_counter:D5}"), AssetType.Laptop));
        await DbContext.SaveChangesAsync();

        var handler = Resolve<DecommissionAssetHandler>();
        await handler.ExecuteAsync(new DecommissionAssetCommand(freshId));
    }

    [Benchmark(Description = "Decommission — asset bloqué par tickets actifs (DomainException)")]
    public async Task Decommission_Blocked()
    {
        var handler = Resolve<DecommissionAssetHandler>();
        try
        {
            await handler.ExecuteAsync(new DecommissionAssetCommand(_blockedAssetId));
        }
        catch (DomainException)
        {
            // Attendu — on mesure le coût du chemin bloquant
        }
    }
}