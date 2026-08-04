using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.UpdateTeam;

public class UpdateTeamCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateTeamCommand, TeamResponseDto>
{
    public async Task<TeamResponseDto> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Team.GetByIdAsync(request.TeamId, cancellationToken) ?? throw new DomainException($"Le team avec l'ID {request.TeamId} est introuvable.");

        // Même contrainte d'unicité qu'à la création : un renommage vers un nom déjà pris
        // ne doit pas attendre la violation de l'index pour être signalé.
        if (!string.IsNullOrWhiteSpace(request.Name) && !string.Equals(request.Name.Trim(), team.Name, StringComparison.OrdinalIgnoreCase))
        {
            var homonyme = await unitOfWork.Team.GetByNameAsync(request.Name.Trim(), cancellationToken);
            if (homonyme is not null && homonyme.Id != team.Id)
                throw new DomainException($"Une équipe nommée '{request.Name.Trim()}' existe déjà.");
        }

        team.Update(request.Name, request.Description, request.AssetType, request.TicketCriticality);

        // L'équipe est lue sans suivi de modifications : sans passage explicite par le dépôt,
        // la mutation ne serait ni persistée ni répercutée sur le cache.
        await unitOfWork.Team.UpdateAsync(team, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }
}
