namespace AssetFlowCore.Application.Interfaces;

/// <summary>
/// Identité de l'utilisateur authentifié pour la requête courante, telle que portée par les
/// revendications du jeton JWT (Lot 7). Aucune dépendance ASP.NET dans la signature : l'implémentation
/// (lecture de `HttpContext.User`) vit dans `AssetFlowCore.Infrastructure`.
/// </summary>
public interface IAuthenticatedUserAccessor
{
    /// <summary>Revendication `oid` (identifiant stable de l'annuaire).</summary>
    string ExternalId { get; }

    string DisplayName { get; }

    string? Email { get; }
}
