using AssetFlowCore.Application.DTOs;

namespace AssetFlowCore.Application.Interfaces;

public interface INotificationService
{
    Task NotifyTeamNewTicketAsync(string teamName, TicketResponseDto ticket);
}
