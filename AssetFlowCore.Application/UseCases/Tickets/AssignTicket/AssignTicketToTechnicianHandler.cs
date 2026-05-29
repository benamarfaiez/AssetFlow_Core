using System;
using System.Threading.Tasks;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.Exceptions;

namespace AssetFlowCore.Application.UseCases.Tickets.AssignTicket;

public class AssignTicketToTechnicianHandler
{
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTicketToTechnicianHandler(IMaintenanceTicketRepository ticketRepository, IAssetRepository assetRepository, IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(AssignTicketToTechnicianCommand command)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);
        if (ticket == null) throw new DomainException("Ticket introuvable.");

        var asset = await _assetRepository.GetByIdAsync(ticket.AssetId);
        if (asset == null) throw new DomainException("Actif lié introuvable.");

        ticket.AssignToTechnician();
        asset.MarkInMaintenance(); // Automate d'état en cascade

        await _unitOfWork.SaveChangesAsync(); // Si conflit de concurrence -> Déclenche DbUpdateConcurrencyException
    }
}