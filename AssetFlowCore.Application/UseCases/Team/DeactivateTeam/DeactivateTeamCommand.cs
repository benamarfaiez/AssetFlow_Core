using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.DeactivateTeam;

public record DeactivateTeamCommand(Guid TeamId) : IRequest<TeamResponseDto>;
