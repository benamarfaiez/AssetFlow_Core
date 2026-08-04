using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTickets;

public class GetTicketsHandler(IMaintenanceTicketRepository ticketRepository)
    : IRequestHandler<GetTicketsQuery, PagedResultDto<TicketResponseDto>>
{
    public async Task<PagedResultDto<TicketResponseDto>> Handle(GetTicketsQuery query, CancellationToken cancellationToken)
    {
        // Les valeurs ont déjà été contrôlées par le validateur : la conversion ne peut plus échouer.
        var criteria = new TicketSearchCriteria(
            Status: ParseOrNull<TicketStatus>(query.Status),
            Criticality: ParseOrNull<TicketCriticality>(query.Criticality),
            AssignedTeamId: query.TeamId,
            AssetId: query.AssetId,
            SortBy: ParseOrNull<TicketSortField>(query.SortBy) ?? TicketSortField.CreatedAt,
            SortDescending: query.SortDescending,
            Page: query.Page,
            PageSize: query.PageSize);

        var page = await ticketRepository.SearchAsync(criteria, cancellationToken);

        // L'équipe est chargée avec l'incident : le mapping ne provoque aucune requête par ligne.
        var items = page.Items
            .Select(ticket => ticket.ToDto(ticket.AssignedTeam?.Name ?? string.Empty))
            .ToList();

        return new PagedResultDto<TicketResponseDto>(items, query.Page, query.PageSize, page.TotalCount);
    }

    private static TEnum? ParseOrNull<TEnum>(string? value) where TEnum : struct, Enum
        => string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<TEnum>(value, ignoreCase: true);
}
