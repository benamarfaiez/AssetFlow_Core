using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;

namespace AssetFlowCore.Domain.Entities;

public class MaintenanceTicket
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TicketCriticality Criticality { get; private set; }
    public TicketStatus Status { get; private set; }
    public string AssignedTeam { get; private set; }
    public string? ResolutionComment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private MaintenanceTicket() { }

    public MaintenanceTicket(Guid id, Guid assetId, string title, string description, TicketCriticality criticality, string assignedTeam)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Le titre est obligatoire.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("La description est obligatoire.");
        if (string.IsNullOrWhiteSpace(assignedTeam)) throw new ArgumentException("L'équipe assignée est obligatoire.");

        Id = id;
        AssetId = assetId;
        Title = title.Trim();
        Description = description.Trim();
        Criticality = criticality;
        AssignedTeam = assignedTeam;
        Status = TicketStatus.Opened;
        CreatedAt = DateTime.UtcNow;
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
            throw new ArgumentException("Un commentaire de résolution est obligatoire pour clôturer le ticket.");

        ResolutionComment = resolutionComment.Trim();
        Status = TicketStatus.Closed;
    }

    public void TransferToTeam(string targetTeam, string reason)
    {
        if (Status == TicketStatus.Closed)
            throw new DomainException("Impossible de transférer un ticket clôturé.");

        if (string.Equals(AssignedTeam, targetTeam, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Le ticket est déjà assigné à l'équipe '{targetTeam}'.");

        AssignedTeam = targetTeam;
        Description += $"\n\n---\n\n**Motif du transfert :** {reason}";
    }
}