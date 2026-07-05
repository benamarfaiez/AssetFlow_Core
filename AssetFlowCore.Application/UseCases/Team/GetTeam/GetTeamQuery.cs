using AssetFlowCore.Application.DTOs;
using MediatR;
namespace AssetFlowCore.Application.UseCases.Team.GetTeam;

public record GetTeamQuery(Guid TeamId) : IRequest<TeamResponseDto>;
