using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.Interfaces;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks;

/// <summary>
/// Implémentation no-op de INotificationService.
/// Remplace SignalR dans les benchmarks pour isoler uniquement
/// la logique métier et la couche de persistance.
/// </summary>
public sealed class NoOpNotificationService : INotificationService
{
    public Task NotifyTeamNewTicketAsync(string teamName, TicketResponseDto ticket)
        => Task.CompletedTask;
}