using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.ActivateTeam;

public record ActivateTeamCommand(Guid TeamId) : IRequest<TeamResponseDto>;
