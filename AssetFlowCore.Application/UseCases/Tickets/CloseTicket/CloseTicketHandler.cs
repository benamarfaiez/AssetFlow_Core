using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.CloseTicket;

public class CloseTicketHandler(IMaintenanceTicketRepository ticketRepository, IAssetRepository assetRepository, IUnitOfWork unitOfWork) : IRequestHandler<CloseTicketCommand>
{
    public async Task Handle(CloseTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.TicketId, cancellationToken) ?? throw new DomainException("Ticket introuvable.");
        var asset = await assetRepository.GetByIdAsync(ticket.AssetId, cancellationToken) ?? throw new DomainException("Actif associé introuvable.");

        ticket.Close(command.ResolutionComment);

        // Automate d'état en cascade : On libère l'appareil si et seulement s'il n'y a plus de pannes en cours
        // Vérifie s'il existe d'autres tickets actifs pour cet actif
        bool hasOtherActive = await ticketRepository.HasOtherActiveTicketsAsync(asset.Id, ticket.Id, cancellationToken);
        if (!hasOtherActive)
        {
            asset.RestoreToService();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
