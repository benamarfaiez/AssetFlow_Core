using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTicket;

public class GetTicketHandler(IMaintenanceTicketRepository ticketRepository, ITeamRepository teamRepository) : IRequestHandler<GetTicketQuery, TicketResponseDto>
{
    public async Task<TicketResponseDto> Handle(GetTicketQuery query, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(query.TicketId, cancellationToken) ?? throw new DomainException($"Le ticket avec l'ID {query.TicketId} est introuvable.");
        var team = await teamRepository.GetByIdAsync(ticket.AssignedTeamId, cancellationToken) ?? throw new DomainException($"Le team avec l'ID {ticket.AssignedTeamId} est introuvable.");

        var dto = ticket.ToDto(team.Name);
        return dto;
    }
}
