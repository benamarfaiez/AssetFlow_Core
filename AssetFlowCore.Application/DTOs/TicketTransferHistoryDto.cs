namespace AssetFlowCore.Application.DTOs;

/// <summary>
/// Entrée de l'historique de transferts d'un incident (décision 0.5).
/// </summary>
public record TicketTransferHistoryDto(
    Guid FromTeamId,
    string FromTeamName,
    Guid ToTeamId,
    string ToTeamName,
    string Reason,
    DateTime TransferredAt);
