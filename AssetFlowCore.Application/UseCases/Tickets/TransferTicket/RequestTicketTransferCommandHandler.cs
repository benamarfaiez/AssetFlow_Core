using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Tickets.TransferTicket;

public class RequestTicketTransferCommandHandler(IMaintenanceTicketRepository ticketRepository, IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(RequestTicketTransferCommand command)
    {
        var ticket = await ticketRepository.GetByIdWithTrackingAsync(command.TicketId) ?? throw new DomainException("Ticket introuvable.");

        ticket.TransferToTeam(command.TargetTeam, command.Reason);
        await unitOfWork.SaveChangesAsync();
    }
}