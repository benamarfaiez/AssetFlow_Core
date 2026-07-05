using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.UpdateTeam;

public class UpdateTeamCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateTeamCommand, TeamResponseDto>
{
    public async Task<TeamResponseDto> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Team.GetByIdAsync(request.TeamId) ?? throw new DomainException($"Le team avec l'ID {request.TeamId} est introuvable.");

        team.Update(request.Name, request.Description, request.AssetType, request.TicketCriticality);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }
}
