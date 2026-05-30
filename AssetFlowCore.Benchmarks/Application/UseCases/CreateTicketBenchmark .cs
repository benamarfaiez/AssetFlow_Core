using AssetFlowCore.Application.DTOs;
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
/// Mesure le pipeline complet de création de ticket :
/// récupération asset → validation domaine → résolution stratégie →
/// mutation état → persistance → notification no-op → mapping DTO.
/// C'est le cas d'utilisation le plus complexe de l'application.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreateTicketBenchmark : BenchmarkBase
{
    private Guid _serverAssetId;
    private Guid _laptopAssetId;
    private Guid _networkAssetId;
    private int _counter;

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices("Bench_CreateTicket");

        _serverAssetId = Guid.NewGuid();
        _laptopAssetId = Guid.NewGuid();
        _networkAssetId = Guid.NewGuid();

        DbContext.Assets.AddRange(
            new Asset(_serverAssetId, "Serveur-Bench", SerialNumber.Create("SRV-BENCH-01"), AssetType.Server),
            new Asset(_laptopAssetId, "Laptop-Bench", SerialNumber.Create("LPT-BENCH-01"), AssetType.Laptop),
            new Asset(_networkAssetId, "Switch-Bench", SerialNumber.Create("SWI-BENCH-01"), AssetType.NetworkDevice)
        );
        await DbContext.SaveChangesAsync();
    }

    [IterationSetup]
    public void ResetAssetStatus()
    {
        // Remet les assets en InService entre chaque itération
        // (MarkAsDown() bloque un 2ème ticket sur le même asset)
        foreach (var asset in DbContext.Assets)
            asset.RestoreToService();
        DbContext.SaveChanges();
        _counter++;
    }

    [Benchmark(Baseline = true, Description = "Ticket Server High → Infrastructure-Serveurs")]
    public async Task<TicketResponseDto> CreateTicket_Server_High()
    {
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            _serverAssetId, $"Panne serveur {_counter}", "Disque HS", "High"));
    }

    [Benchmark(Description = "Ticket Laptop High → Support-VIP")]
    public async Task<TicketResponseDto> CreateTicket_Laptop_High()
    {
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            _laptopAssetId, $"Laptop VIP {_counter}", "Écran cassé", "High"));
    }

    [Benchmark(Description = "Ticket Laptop Medium → Support-Lectorat")]
    public async Task<TicketResponseDto> CreateTicket_Laptop_Medium()
    {
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            _laptopAssetId, $"Laptop Standard {_counter}", "Clavier HS", "Medium"));
    }

    [Benchmark(Description = "Ticket Network Low → Réseau-Télécom")]
    public async Task<TicketResponseDto> CreateTicket_Network_Low()
    {
        var handler = Resolve<CreateMaintenanceTicketHandler>();
        return await handler.HandleAsync(new CreateMaintenanceTicketCommand(
            _networkAssetId, $"Switch {_counter}", "Port défaillant", "Low"));
    }
}