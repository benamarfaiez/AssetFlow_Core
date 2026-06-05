using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTicket;

public class GetTicketHandler(IMaintenanceTicketRepository ticketRepository, ITeamRepository teamRepository)
{
    public async Task<TicketResponseDto> ExecuteAsync(GetTicketQuery query)
    {
        var ticket = await ticketRepository.GetByIdAsync(query.TicketId) ?? throw new DomainException($"Le ticket avec l'ID {query.TicketId} est introuvable.");
        var team = await teamRepository.GetByIdAsync(ticket.AssignedTeamId) ?? throw new DomainException($"Le team avec l'ID {query.TicketId} est introuvable.");

        var dto = ticket.ToDto(team.Name);
        return dto;
    }
}