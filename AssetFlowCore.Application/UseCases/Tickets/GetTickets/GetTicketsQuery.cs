using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTickets;

/// <summary>
/// Recherche d'incidents. Tous les filtres sont facultatifs et se cumulent ; les valeurs
/// d'énumération sont transmises par leur nom, la casse étant indifférente.
/// </summary>
/// <param name="SortBy">`CreatedAt` (défaut), `Criticality`, `Status` ou `Title`.</param>
/// <param name="SortDescending">Ordre décroissant par défaut : les incidents les plus récents d'abord.</param>
public record GetTicketsQuery(
    string? Status = null,
    string? Criticality = null,
    Guid? TeamId = null,
    Guid? AssetId = null,
    string? SortBy = null,
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResultDto<TicketResponseDto>>;
