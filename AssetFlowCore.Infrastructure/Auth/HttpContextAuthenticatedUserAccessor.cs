using System.Security.Claims;
using AssetFlowCore.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AssetFlowCore.Infrastructure.Auth;

/// <summary>
/// Lit l'identité de l'utilisateur authentifié depuis les revendications du jeton JWT courant
/// (Lot 7). N'est sollicitée que derrière un endpoint protégé par <c>[Authorize]</c> : les
/// revendications sont donc toujours présentes lorsque cette classe est utilisée.
/// </summary>
public class HttpContextAuthenticatedUserAccessor(IHttpContextAccessor httpContextAccessor) : IAuthenticatedUserAccessor
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("Aucun contexte HTTP authentifié n'est disponible.");

    public string ExternalId =>
        FindClaim("oid") ?? FindClaim(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Le jeton ne porte aucune revendication d'identifiant (oid).");

    public string DisplayName =>
        FindClaim("name") ?? FindClaim("preferred_username") ?? Email ?? ExternalId;

    public string? Email => FindClaim("preferred_username") ?? FindClaim(ClaimTypes.Email);

    private string? FindClaim(string claimType) => User.FindFirst(claimType)?.Value;
}
