using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Team.UpdateTeam;

public class UpdateTeamCommandHandler(IUnitOfWork unitOfWork)
{
    public async Task<TeamResponseDto> HandleAsync(UpdateTeamCommand request)
    {
        var team = await unitOfWork.Team.GetByIdAsync(request.TeamId) ?? throw new DomainException($"Le team avec l'ID {request.TeamId} est introuvable.");

        team.Update(request.Name, request.Description, request.AssetType, request.TicketCriticality);

        await unitOfWork.SaveChangesAsync();

        return team.ToDto();
    }
}