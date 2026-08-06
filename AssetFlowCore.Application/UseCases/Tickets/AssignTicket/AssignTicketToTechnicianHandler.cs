using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.AssignTicket;

public class AssignTicketToTechnicianHandler(
    IMaintenanceTicketRepository ticketRepository,
    IAssetRepository assetRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork) : IRequestHandler<AssignTicketToTechnicianCommand>
{
    private readonly IMaintenanceTicketRepository _ticketRepository = ticketRepository;
    private readonly IAssetRepository _assetRepository = assetRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task Handle(AssignTicketToTechnicianCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken)
            ?? throw NotFoundException.For("L'incident", command.TicketId);
        var asset = await _assetRepository.GetByIdAsync(ticket.AssetId, cancellationToken) ?? throw new DomainException("Actif lié introuvable.");
        var assignedByUserId = await currentUserService.GetOrCreateUserIdAsync(cancellationToken);
        ticket.AssignToTechnician(assignedByUserId);
        asset.MarkInMaintenance(); // Automate d'état en cascade

        await _unitOfWork.SaveChangesAsync(cancellationToken); // Si conflit de concurrence -> Déclenche DbUpdateConcurrencyException
    }
}
