using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Team.GetTeam;

public class GetTeamHandler(ITeamRepository teamRepository)
{
    public async Task<TeamResponseDto> ExecuteAsync(GetTeamQuery query)
    {
        var team = await teamRepository.GetByIdAsync(query.TeamId) ?? throw new DomainException($"Le team avec l'ID {query.TeamId} est introuvable.");

        var dto = team.ToDto();
        return dto;
    }
}