using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTicket;

public class GetTicketHandler(IMaintenanceTicketRepository ticketRepository)
{
    public async Task<TicketResponseDto> ExecuteAsync(GetTicketQuery query)
    {
        var ticket = await ticketRepository.GetByIdAsync(query.TicketId) ?? throw new DomainException($"Le ticket avec l'ID {query.TicketId} est introuvable.");

        var dto = ticket.ToDto();
        return dto;
    }
}