// Contrat synchronisé depuis le backend .NET — ne pas modifier à la main.
// Source : AssetFlowCore.Application/DTOs/PagedResultDto.cs
// Resynchroniser avec : /sync-api-dtos AssetFlowCore.Application/DTOs/PagedResultDto.cs

/**
 * `PagedResultDto<T>` — enveloppe de pagination.
 *
 * `totalCount` est indispensable pour dimensionner une navigation : il ne se déduit pas de la
 * page reçue. `totalPages` est une propriété calculée côté C# (`TotalPages`), donc bien
 * présente dans la charge utile ; elle vaut 0 lorsqu'aucun élément ne correspond.
 *
 * Seul `GET /api/tickets` est paginé : l'inventaire et les équipes sont servis en intégralité.
 */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}
