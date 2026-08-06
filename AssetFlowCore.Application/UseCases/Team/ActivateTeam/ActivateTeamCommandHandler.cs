using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.ActivateTeam;

public class ActivateTeamCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ActivateTeamCommand, TeamResponseDto>
{
    public async Task<TeamResponseDto> Handle(ActivateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Team.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw NotFoundException.For("L'équipe", request.TeamId);

        team.Activate();

        // L'équipe est lue sans suivi de modifications : sans passage explicite par le dépôt,
        // la mutation ne serait ni persistée ni répercutée sur le cache (cf. UpdateTeamCommandHandler).
        await unitOfWork.Team.UpdateAsync(team, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }
}
