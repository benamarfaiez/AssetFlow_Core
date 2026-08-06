using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.TransferTicket;

public class RequestTicketTransferCommandHandler(IMaintenanceTicketRepository ticketRepository, ITeamRepository teamRepository, IUnitOfWork unitOfWork) : IRequestHandler<RequestTicketTransferCommand>
{
    public async Task Handle(RequestTicketTransferCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithTrackingAsync(command.TicketId, cancellationToken)
            ?? throw NotFoundException.For("L'incident", command.TicketId);

        // L'équipe cible est une donnée du corps, pas la ressource visée par l'URI :
        // une valeur inconnue est un refus métier (400), pas une ressource absente (404).
        var team = await teamRepository.GetByNameAsync(command.TeamName, cancellationToken)
            ?? throw new DomainException($"L'équipe '{command.TeamName}' n'existe pas ou n'est plus active.");
        var historyEntry = ticket.TransferToTeam(team, command.Reason);
        await ticketRepository.AddTransferHistoryAsync(historyEntry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
