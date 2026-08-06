using System.Security.Claims;
using System.Text.Encodings.Web;
using AssetFlowCore.WebApi.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssetFlowCore.IntegrationTests.WebApi;

/// <summary>
/// Schéma d'authentification de test qui remplace le schéma JWT Bearer réel (Lot 7) dans
/// <see cref="CustomWebApplicationFactory{TProgram}"/>. Authentifie par défaut un utilisateur
/// disposant de tous les rôles, pour que les tests existants continuent d'exercer le pipeline
/// HTTP réel sans jeton Entra ID. Deux en-têtes pilotent les scénarios d'autorisation :
/// <see cref="RolesHeader"/> pour simuler un rôle restreint (403), <see cref="UnauthenticatedHeader"/>
/// pour simuler l'absence de jeton (401).
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RolesHeader = "X-Test-Roles";
    public const string UnauthenticatedHeader = "X-Test-Unauthenticated";

    public static readonly Guid DefaultUserExternalId = Guid.Parse("00000000-0000-0000-0000-0000000000ab");

    private static readonly string[] AllRoles =
    [
        Roles.Administrateur,
        Roles.Technicien,
        Roles.GestionnaireDeParc,
        Roles.ResponsableEquipe,
    ];

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(UnauthenticatedHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var roles = Request.Headers.TryGetValue(RolesHeader, out var rolesHeader)
            ? rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : AllRoles;

        var claims = new List<Claim>
        {
            new("oid", DefaultUserExternalId.ToString()),
            new("name", "Utilisateur de test"),
            new("preferred_username", "test@assetflowcore.local"),
        };
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        var identity = new ClaimsIdentity(claims, SchemeName, nameType: "name", roleType: "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
