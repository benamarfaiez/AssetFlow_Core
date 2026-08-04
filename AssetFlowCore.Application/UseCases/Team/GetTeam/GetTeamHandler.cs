using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.GetTeam;

public class GetTeamHandler(ITeamRepository teamRepository) : IRequestHandler<GetTeamQuery, TeamResponseDto>
{
    public async Task<TeamResponseDto> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(query.TeamId, cancellationToken)
            ?? throw NotFoundException.For("L'équipe", query.TeamId);

        var dto = team.ToDto();
        return dto;
    }
}
