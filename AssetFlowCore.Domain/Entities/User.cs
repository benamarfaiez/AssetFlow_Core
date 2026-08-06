namespace AssetFlowCore.Domain.Entities;

/// <summary>
/// Utilisateur authentifié via l'annuaire d'entreprise (Lot 7, décision 0.1). Provisionné
/// « just-in-time » à la première requête authentifiée d'une identité encore inconnue.
/// </summary>
public class User
{
    public Guid Id { get; private set; }

    /// <summary>Identifiant stable de l'annuaire (revendication `oid` du jeton Entra ID).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    /// <summary>Équipe de rattachement. Nullable : l'affectation opérationnelle est un prérequis du Lot 6.6, hors périmètre du Lot 7.</summary>
    public Guid? TeamId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Constructeur privé requis par EF Core pour la matérialisation
    private User()
    {
        ExternalId = null!;
        DisplayName = null!;
    }

    public User(Guid id, string externalId, string displayName, string? email)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("L'identifiant externe est obligatoire.", nameof(externalId));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Le nom affiché est obligatoire.", nameof(displayName));

        Id = id;
        ExternalId = externalId.Trim();
        DisplayName = displayName.Trim();
        Email = email?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void AssignToTeam(Guid teamId) => TeamId = teamId;
}
