using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.CreateTeam;

public class CreateTeamCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateTeamCommand, TeamResponseDto>
{
    public async Task<TeamResponseDto> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = new Domain.Entities.Team(request.Name, request.AssetType, request.TicketCriticality, request.Description);

        // Le nom porte un index unique en base : sans ce contrôle, le doublon ne se manifestait
        // qu'à la persistance, sous la forme d'une violation d'index remontée en 500.
        if (await unitOfWork.Team.ExistsWithNameAsync(team.Name, cancellationToken))
            throw new DomainException($"Une équipe nommée '{team.Name}' existe déjà.");

        await unitOfWork.Team.AddAsync(team, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }
}
