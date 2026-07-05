using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.CreateTeam;

public class CreateTeamCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateTeamCommand, TeamResponseDto>
{
    public async Task<TeamResponseDto> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = new Domain.Entities.Team(request.Name, request.AssetType, request.TicketCriticality, request.Description);

        await unitOfWork.Team.AddAsync(team);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }
}
