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

    public void AssignToTechnician()
    {
        if (Status != TicketStatus.Opened)
            throw new InvalidOperationException("Seul un ticket ouvert peut être pris en charge.");

        Status = TicketStatus.InProgress;
    }

    public void Close(string resolutionComment)
    {
        if (Status != TicketStatus.InProgress)
            throw new InvalidOperationException("Seul un ticket en cours peut être clôturé.");

        if (string.IsNullOrWhiteSpace(resolutionComment))
            throw new ArgumentException("Un commentaire de résolution est obligatoire.");

        ResolutionComment = resolutionComment.Trim();
        Status = TicketStatus.Closed;
    }

    public void TransferToTeam(Team team, string reason)
    {
        if (Status == TicketStatus.Closed)
            throw new DomainException("Impossible de transférer un ticket clôturé.");

        if (AssignedTeamId == team.Id)
            throw new DomainException($"Le ticket est déjà assigné à l'équipe '{team.Name}'.");

        AssignedTeam = team;
        Description += $"\n\n---\n\n**Motif du transfert :** {reason}";
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