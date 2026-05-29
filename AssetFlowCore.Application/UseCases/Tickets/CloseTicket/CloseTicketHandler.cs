using System;
using System.Threading.Tasks;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.Exceptions;

namespace AssetFlowCore.Application.UseCases.Tickets.CloseTicket;

public class CloseTicketHandler
{
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseTicketHandler(IMaintenanceTicketRepository ticketRepository, IAssetRepository assetRepository, IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(CloseTicketCommand command)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);
        if (ticket == null) throw new DomainException("Ticket introuvable.");

        var asset = await _assetRepository.GetByIdAsync(ticket.AssetId);
        if (asset == null) throw new DomainException("Actif associé introuvable.");

        ticket.Close(command.ResolutionComment);

        // Automate d'état en cascade : On libère l'appareil si et seulement s'il n'y a plus de pannes en cours
        int remainingActiveTickets = await _ticketRepository.CountActiveTicketsByAssetIdAsync(asset.Id);
        if (remainingActiveTickets <= 1) // 1 correspond au ticket en cours de clôture
        {
            asset.RestoreToService();
        }

        await _unitOfWork.SaveChangesAsync();
    }
}