namespace AssetFlowCore.Domain.Entities;

public class Team
{
    public Guid Id { get; private set; }

    /// <summary>Nom unique de l'équipe (ex: "Infrastructure-Serveurs", "Support-VIP").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Description du périmètre de responsabilité de l'équipe.</summary>
    public string? Description { get; private set; }

    /// <summary>Indique si l'équipe est active et peut recevoir de nouveaux tickets.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Date de création de l'équipe dans le système.</summary>
    public DateTime CreatedAt { get; private set; }
    public string AssetType { get; private set; }
    public string TicketCriticality { get; private set; }

    /// <summary>Tickets actuellement assignés à cette équipe.</summary>
    public IReadOnlyCollection<MaintenanceTicket> Tickets => _tickets.AsReadOnly();
    private readonly List<MaintenanceTicket> _tickets = [];

    // Constructeur privé requis par EF Core pour la matérialisation
    private Team()
    {
        AssetType = null!;
        TicketCriticality = null!;
    }

    public Team(string name, string assetType, string ticketCriticality, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom de l'équipe est obligatoire.", nameof(name));
        if (string.IsNullOrWhiteSpace(assetType))
            throw new ArgumentException("Le assetType de l'équipe est obligatoire.", nameof(assetType));
        if (string.IsNullOrWhiteSpace(ticketCriticality))
            throw new ArgumentException("Le ticketCriticality de l'équipe est obligatoire.", nameof(ticketCriticality));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        IsActive = true;
        AssetType = assetType.Trim();
        TicketCriticality = ticketCriticality.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void UpdateDescription(string description)
        => Description = description?.Trim();

    public void Update(string? name, string? description, string? assetType, string? ticketCriticality)
    {
        if (name != null) Name = name;
        if (description != null) Description = description.Trim();
        if (assetType != null) AssetType = assetType.Trim();
        if (ticketCriticality != null) TicketCriticality = ticketCriticality.Trim();
    }
}