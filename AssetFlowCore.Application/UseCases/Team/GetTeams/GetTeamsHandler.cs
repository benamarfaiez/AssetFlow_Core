using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.GetTeams;

public class GetTeamsHandler(ITeamRepository teamRepository)
    : IRequestHandler<GetTeamsQuery, IReadOnlyCollection<TeamResponseDto>>
{
    public async Task<IReadOnlyCollection<TeamResponseDto>> Handle(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var teams = query.OnlyActive
            ? await teamRepository.GetAllActiveAsync(cancellationToken)
            : await teamRepository.GetAllAsync(cancellationToken);

        return [.. teams.Select(team => team.ToDto())];
    }
}
