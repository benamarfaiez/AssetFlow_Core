using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AssetFlowCore.Infrastructure.Notifications;

public class SignalRNotificationService(IHubContext<TicketHub> hubContext) : INotificationService
{
    public async Task NotifyTeamNewTicketAsync(string teamName, TicketResponseDto ticket)
    {
        // Diffuse le message exclusivement au groupe de l'équipe assignée
        await hubContext.Clients.Group(teamName).SendAsync("ReceiveNewTicket", ticket);
    }
}
