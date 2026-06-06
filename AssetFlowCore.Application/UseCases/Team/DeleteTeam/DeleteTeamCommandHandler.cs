using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Team.DeleteTeam;

public class DeleteTeamCommandHandler(ITeamRepository teamRepository, IMaintenanceTicketRepository ticketRepository, IUnitOfWork unitOfWork)
{
    public async ValueTask ExecuteAsync(DeleteTeamCommand command)
    {
        var team = await teamRepository.GetByIdAsync(command.TeamId) ?? throw new DomainException("Team introuvable.");

        // Vérifier côté DB s'il existe des tickets actifs assignés à cette équipe
        bool hasActive = await ticketRepository.ExistsActiveTicketsForTeamAsync(team.Id);
        if (hasActive)
            throw new DomainException("Impossible de supprimer l'équipe : des tickets actifs lui sont assignés.");

        await teamRepository.RemoveAsync(team);
        await unitOfWork.SaveChangesAsync();
    }
}
