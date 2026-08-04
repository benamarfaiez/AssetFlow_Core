using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Domain.Repositories;

/// <summary>Champ de tri accepté par la recherche d'incidents.</summary>
public enum TicketSortField
{
    /// <summary>Date d'ouverture — tri par défaut, du plus récent au plus ancien.</summary>
    CreatedAt,
    Criticality,
    Status,
    Title
}

/// <summary>
/// Critères de recherche d'incidents. Chaque filtre est facultatif ; les filtres fournis
/// se cumulent (ET logique).
/// </summary>
/// <param name="Page">Numéro de page, à partir de 1.</param>
/// <param name="PageSize">Nombre d'éléments par page.</param>
public record TicketSearchCriteria(
    TicketStatus? Status = null,
    TicketCriticality? Criticality = null,
    Guid? AssignedTeamId = null,
    Guid? AssetId = null,
    TicketSortField SortBy = TicketSortField.CreatedAt,
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// Page de résultats accompagnée du décompte total, indissociables : le total ne peut pas
/// se déduire d'une page et impose sa propre requête d'agrégation.
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
