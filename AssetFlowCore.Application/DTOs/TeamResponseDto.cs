namespace AssetFlowCore.Application.DTOs;

/// <summary>
/// Représentation d'une équipe technique.
/// </summary>
/// <param name="AssetType">Type d'actif pris en charge — nom d'une valeur de <c>AssetType</c>.</param>
/// <param name="TicketCriticality">Criticité prise en charge — nom d'une valeur de <c>TicketCriticality</c>.</param>
public record TeamResponseDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    string AssetType,
    string TicketCriticality);
