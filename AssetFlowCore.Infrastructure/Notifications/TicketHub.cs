using Microsoft.AspNetCore.SignalR;

namespace AssetFlowCore.Infrastructure.Notifications;

public class TicketHub : Hub
{
    public async Task JoinTeamGroup(string teamName) => await Groups.AddToGroupAsync(Context.ConnectionId, teamName);
}