using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.DeleteTeam;

public class DeleteTeamCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTeamCommand>
{
    public async Task Handle(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Team.GetByIdAsync(command.TeamId) ?? throw new DomainException("Team introuvable.");

        // Vérifier côté DB s'il existe des tickets actifs assignés à cette équipe
        bool hasActive = await unitOfWork.MaintenanceTicket.ExistsActiveTicketsForTeamAsync(team.Id);
        if (hasActive)
            throw new DomainException("Impossible de supprimer le team : des tickets actifs lui sont assignes.");

        await unitOfWork.Team.RemoveAsync(team);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
