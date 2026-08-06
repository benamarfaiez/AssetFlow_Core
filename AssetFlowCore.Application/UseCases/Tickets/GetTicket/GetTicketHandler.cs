using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTicket;

public class GetTicketHandler(IMaintenanceTicketRepository ticketRepository, ITeamRepository teamRepository) : IRequestHandler<GetTicketQuery, TicketResponseDto>
{
    public async Task<TicketResponseDto> Handle(GetTicketQuery query, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(query.TicketId, cancellationToken)
            ?? throw NotFoundException.For("L'incident", query.TicketId);
        var team = await teamRepository.GetByIdAsync(ticket.AssignedTeamId, cancellationToken) ?? throw new DomainException($"Le team avec l'ID {ticket.AssignedTeamId} est introuvable.");

        var history = await ticketRepository.GetTransferHistoryAsync(ticket.Id, cancellationToken);
        ticket.LoadTransferHistory(history);

        var transferHistory = new List<TicketTransferHistoryDto>();
        foreach (var entry in ticket.TransferHistory)
        {
            var fromTeam = await teamRepository.GetByIdAsync(entry.FromTeamId, cancellationToken);
            var toTeam = await teamRepository.GetByIdAsync(entry.ToTeamId, cancellationToken);
            transferHistory.Add(new TicketTransferHistoryDto(
                entry.FromTeamId,
                fromTeam?.Name ?? "Équipe inconnue",
                entry.ToTeamId,
                toTeam?.Name ?? "Équipe inconnue",
                entry.Reason,
                entry.TransferredAt));
        }

        var dto = ticket.ToDto(team.Name, transferHistory);
        return dto;
    }
}
