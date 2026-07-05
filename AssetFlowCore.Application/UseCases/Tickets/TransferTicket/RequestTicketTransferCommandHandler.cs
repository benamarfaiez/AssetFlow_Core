using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.TransferTicket;

public class RequestTicketTransferCommandHandler(IMaintenanceTicketRepository ticketRepository, ITeamRepository teamRepository, IUnitOfWork unitOfWork) : IRequestHandler<RequestTicketTransferCommand>
{
    public async Task Handle(RequestTicketTransferCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithTrackingAsync(command.TicketId) ?? throw new DomainException("Ticket introuvable.");
        var team = await teamRepository.GetByNameAsync(command.TeamName) ?? throw new DomainException("Équipe introuvable.");
        ticket.TransferToTeam(team, command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
