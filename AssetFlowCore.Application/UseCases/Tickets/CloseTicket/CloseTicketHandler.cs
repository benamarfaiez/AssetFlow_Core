using System;
using System.Threading.Tasks;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.Exceptions;

namespace AssetFlowCore.Application.UseCases.Tickets.CloseTicket;

public class CloseTicketHandler(IMaintenanceTicketRepository ticketRepository, IAssetRepository assetRepository, IUnitOfWork unitOfWork)
{
    public async ValueTask ExecuteAsync(CloseTicketCommand command)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.TicketId) ?? throw new DomainException("Ticket introuvable.");
        var asset = await assetRepository.GetByIdAsync(ticket.AssetId) ?? throw new DomainException("Actif associé introuvable.");

        ticket.Close(command.ResolutionComment);

        // Automate d'état en cascade : On libère l'appareil si et seulement s'il n'y a plus de pannes en cours
        // Vérifie s'il existe d'autres tickets actifs pour cet actif
        bool hasOtherActive = await ticketRepository.HasOtherActiveTicketsAsync(asset.Id, ticket.Id);
        if (!hasOtherActive)
        {
            asset.RestoreToService();
        }

        await unitOfWork.SaveChangesAsync();
    }
}