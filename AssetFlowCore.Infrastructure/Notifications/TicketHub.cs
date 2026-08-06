using AssetFlowCore.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AssetFlowCore.Infrastructure.Notifications;

[Authorize]
public class TicketHub(ITeamRepository teamRepository) : Hub
{
    /// <summary>
    /// Défense en profondeur en attendant le Lot 6.6 (rattachement d'un utilisateur à une équipe) :
    /// la vérification d'appartenance réelle comparera alors <paramref name="teamName"/> à l'équipe
    /// de l'utilisateur courant. En l'état, on refuse au moins de rejoindre un nom d'équipe inexistant
    /// ou inactif.
    /// </summary>
    public async Task JoinTeamGroup(string teamName)
    {
        var team = await teamRepository.GetByNameAsync(teamName, Context.ConnectionAborted);
        if (team is null)
            throw new HubException("Équipe inconnue.");

        await Groups.AddToGroupAsync(Context.ConnectionId, teamName, Context.ConnectionAborted);
    }
}