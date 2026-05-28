using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases;

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

    public async Task ExecuteAsync(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null) throw new DomainException("Ticket introuvable.");

        var asset = await _assetRepository.GetByIdAsync(ticket.AssetId);
        if (asset == null) throw new DomainException("Actif lié introuvable.");

        ticket.AssignToTechnician();
        asset.MarkInMaintenance();

        // Le catch de la DbUpdateConcurrencyException est délégué au middleware d'infrastructure/API
        await _unitOfWork.SaveChangesAsync();
    }
}