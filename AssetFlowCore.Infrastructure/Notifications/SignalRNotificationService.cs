using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AssetFlowCore.Infrastructure.Notifications;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<TicketHub> _hubContext;
    public SignalRNotificationService(IHubContext<TicketHub> hubContext) => _hubContext = hubContext;

    public async Task NotifyTeamNewTicketAsync(string teamName, TicketResponseDto ticket)
    {
        // Diffuse le message exclusivement au groupe de l'équipe assignée
        await _hubContext.Clients.Group(teamName).SendAsync("ReceiveNewTicket", ticket);
    }
}
