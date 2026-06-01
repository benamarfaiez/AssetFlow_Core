using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure le pipeline complet de création de ticket :
/// récupération asset → validation domaine → résolution stratégie →
/// mutation état → persistance → notification no-op → mapping DTO.
/// C'est le cas d'utilisation le plus complexe de l'application.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 10)]
public class CreateTicketBenchmark : BenchmarkBase
{
    private int _counter;

    [GlobalSetup]
    public void Setup() => SetupServices("Bench_CreateTicket");

    private async Task<Guid> CreateFreshAsset(string prefix, AssetType type)
    {
        var id = Guid.NewGuid();
        _counter++;
        DbContext.Assets.Add(new Asset(id, $"{prefix}-{_counter}",
            SerialNumber.Create($"{prefix}-{_counter:D6}"), type));
        await DbContext.SaveChangesAsync();
        return id;
    }

    [Benchmark(Baseline = true, Description = "Ticket Server High → Infrastructure-Serveurs")]
    public async Task<TicketResponseDto> CreateTicket_Server_High()
    {
        var assetId = await CreateFreshAsset("SRV", AssetType.Server);
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            assetId, $"Panne serveur {_counter}", "Disque HS", "High"));
    }

    [Benchmark(Description = "Ticket Laptop High → Support-VIP")]
    public async Task<TicketResponseDto> CreateTicket_Laptop_High()
    {
        var assetId = await CreateFreshAsset("LPT", AssetType.Laptop);
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            assetId, $"Laptop VIP {_counter}", "Écran cassé", "High"));
    }

    [Benchmark(Description = "Ticket Laptop Medium → Support-Lectorat")]
    public async Task<TicketResponseDto> CreateTicket_Laptop_Medium()
    {
        var assetId = await CreateFreshAsset("LPM", AssetType.Laptop);
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            assetId, $"Laptop Standard {_counter}", "Clavier HS", "Medium"));
    }

    [Benchmark(Description = "Ticket Network Low → Réseau-Télécom")]
    public async Task<TicketResponseDto> CreateTicket_Network_Low()
    {
        var assetId = await CreateFreshAsset("SWI", AssetType.NetworkDevice);
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            assetId, $"Switch {_counter}", "Port défaillant", "Low"));
    }
}