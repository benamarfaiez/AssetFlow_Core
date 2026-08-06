using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;

namespace AssetFlowCore.Domain.Entities;

public class MaintenanceTicket
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public TicketCriticality Criticality { get; private set; }
    public TicketStatus Status { get; private set; }

    public Guid AssignedTeamId { get; private set; }
    public Team AssignedTeam { get; private set; } = null!;

    public string? ResolutionComment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public string? AssistanceNote { get; private set; }
    public bool IsAiProcessing { get; private set; }

    /// <summary>Auteur de la prise en charge (décision 0.2) ; <c>null</c> tant que le ticket n'a jamais été pris en charge.</summary>
    public Guid? AssignedByUserId { get; private set; }

    /// <summary>Auteur de la clôture (décision 0.2) ; <c>null</c> tant que le ticket n'est pas clôturé.</summary>
    public Guid? ClosedByUserId { get; private set; }

    /// <summary>Historique des transferts (décision 0.5), du plus ancien au plus récent.</summary>
    public IReadOnlyCollection<TicketTransferHistory> TransferHistory => _transferHistory.AsReadOnly();
    private readonly List<TicketTransferHistory> _transferHistory = [];

    private MaintenanceTicket()
    {
        Description = null!;
        Title = null!;
        Asset = null!;
    }

    public MaintenanceTicket(
        Guid id,
        Guid assetId,
        string title,
        string description,
        TicketCriticality criticality,
        Guid assignedTeamId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Le titre est obligatoire.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description est obligatoire.", nameof(description));
        if (assignedTeamId == Guid.Empty)
            throw new ArgumentException("L'équipe assignée est obligatoire.", nameof(assignedTeamId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("L'actif est obligatoire.", nameof(assetId));

        Id = id;
        AssetId = assetId;
        Title = title.Trim();
        Description = description.Trim();
        Criticality = criticality;
        AssignedTeamId = assignedTeamId;
        Status = TicketStatus.Opened;
        CreatedAt = DateTime.UtcNow;
        IsAiProcessing = true;
        Asset = null!;
    }

    public void AssignToTechnician(Guid assignedByUserId)
    {
        if (Status != TicketStatus.Opened)
            throw new DomainException("Seul un ticket ouvert peut être pris en charge.");
        if (assignedByUserId == Guid.Empty)
            throw new ArgumentException("L'auteur de la prise en charge est obligatoire.", nameof(assignedByUserId));

        Status = TicketStatus.InProgress;
        AssignedByUserId = assignedByUserId;
    }

    public void Close(Guid closedByUserId, string resolutionComment)
    {
        if (Status != TicketStatus.InProgress)
            throw new DomainException("Seul un ticket en cours peut être clôturé.");

        if (string.IsNullOrWhiteSpace(resolutionComment))
            throw new ArgumentException("Un commentaire de résolution est obligatoire.");
        if (closedByUserId == Guid.Empty)
            throw new ArgumentException("L'auteur de la clôture est obligatoire.", nameof(closedByUserId));

        ResolutionComment = resolutionComment.Trim();
        Status = TicketStatus.Closed;
        ClosedByUserId = closedByUserId;
    }

    /// <summary>
    /// Transfère le ticket vers une autre équipe et retourne l'entrée d'historique créée : à
    /// persister explicitement par l'appelant (<c>IMaintenanceTicketRepository.AddTransferHistoryAsync</c>),
    /// <see cref="TransferHistory"/> n'étant pas suivie par EF (voir <see cref="LoadTransferHistory"/>).
    /// </summary>
    public TicketTransferHistory TransferToTeam(Team team, string reason)
    {
        if (Status == TicketStatus.Closed)
            throw new DomainException("Impossible de transférer un ticket clôturé.");

        if (AssignedTeamId == team.Id)
            throw new DomainException($"Le ticket est déjà assigné à l'équipe '{team.Name}'.");

        var entry = new TicketTransferHistory(Id, AssignedTeamId, team.Id, reason);
        _transferHistory.Add(entry);

        AssignedTeamId = team.Id;
        AssignedTeam = team;
        return entry;
    }

    /// <summary>
    /// Hydrate l'historique de transferts depuis une lecture séparée (repository) : la collection
    /// n'est **pas** une navigation suivie par EF Core (voir <c>MaintenanceTicketConfiguration.Ignore</c>),
    /// pour éviter la découverte en cascade d'un nouvel enregistrement d'historique lors du même
    /// <c>SaveChanges</c> qui réaffecte l'équipe — combinaison qui, avec le jeton <see cref="RowVersion"/>,
    /// fait échouer le fournisseur EF InMemory (<c>DbUpdateConcurrencyException</c> à tort).
    /// </summary>
    public void LoadTransferHistory(IEnumerable<TicketTransferHistory> history)
    {
        _transferHistory.Clear();
        _transferHistory.AddRange(history);
    }

    public void SetAssistanceNote(string markdownNote)
    {
        if (string.IsNullOrWhiteSpace(markdownNote))
            throw new ArgumentException("La note d'assistance ne peut pas être vide.", nameof(markdownNote));

        AssistanceNote = markdownNote;
        IsAiProcessing = false;
    }
    public void FailAiProcessing()
    {
        IsAiProcessing = false;
    }
}