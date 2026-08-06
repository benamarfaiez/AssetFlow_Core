namespace AssetFlowCore.Domain.Entities;

/// <summary>
/// Entrée immuable de l'historique de transferts d'un incident (décision 0.5, Lot 2 bis) :
/// une trace factuelle, jamais modifiée après sa création. Les équipes sont référencées par
/// identifiant sans contrainte de clé étrangère : une équipe supprimée après avoir figuré dans
/// un transfert passé ne doit pas bloquer sa propre suppression.
/// </summary>
public class TicketTransferHistory
{
    public Guid Id { get; private set; }
    public Guid MaintenanceTicketId { get; private set; }
    public Guid FromTeamId { get; private set; }
    public Guid ToTeamId { get; private set; }
    public string Reason { get; private set; }
    public DateTime TransferredAt { get; private set; }

    // Pour EF Core
    private TicketTransferHistory()
    {
        Reason = null!;
    }

    public TicketTransferHistory(Guid maintenanceTicketId, Guid fromTeamId, Guid toTeamId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Le motif du transfert est obligatoire.", nameof(reason));

        Id = Guid.NewGuid();
        MaintenanceTicketId = maintenanceTicketId;
        FromTeamId = fromTeamId;
        ToTeamId = toTeamId;
        Reason = reason.Trim();
        TransferredAt = DateTime.UtcNow;
    }
}
