namespace AssetFlowCore.Application.DTOs;

/// <summary>
/// Enveloppe de pagination : le nombre total d'éléments est indispensable au client
/// pour dimensionner sa navigation, il ne peut pas se déduire de la page reçue.
/// </summary>
public record PagedResultDto<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Nombre total de pages ; vaut 0 lorsqu'aucun élément ne correspond.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
