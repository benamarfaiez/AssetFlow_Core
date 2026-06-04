using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Team.CreateTeam;

public class CreateTeamCommandHandler(IUnitOfWork unitOfWork, ITeamRepository teamRepository)
{
    public async Task<TeamResponseDto> HandleAsync(CreateTeamCommand request)
    {
        var team = new Domain.Entities.Team(request.Name, request.AssetType, request.TicketCriticality, request.Description);

        await teamRepository.AddAsync(team);

        await unitOfWork.SaveChangesAsync();

        return team.ToDto();
    }
}
