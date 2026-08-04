namespace AssetFlowCore.Application.DTOs;

/// <summary>
/// Fiche complète d'un actif : ses caractéristiques et l'ensemble de ses incidents,
/// du plus récent au plus ancien.
/// </summary>
public record AssetDetailResponseDto(
    Guid Id,
    string Name,
    string SerialNumber,
    string Type,
    string Status,
    DateTime CreatedAt,
    IReadOnlyCollection<AssetTicketDto> Tickets);

/// <summary>
/// Incident tel que présenté dans la fiche d'un actif : le contexte de l'actif étant
/// déjà porté par la fiche, seuls les éléments propres à l'incident sont repris.
/// </summary>
public record AssetTicketDto(
    Guid Id,
    string Title,
    string Criticality,
    string Status,
    DateTime CreatedAt,
    Guid AssignedTeamId,
    string AssignedTeamName);
