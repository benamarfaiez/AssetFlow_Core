namespace AssetFlowCore.Application.DTOs;

/// <summary>
/// Représentation complète d'un incident de maintenance.
/// </summary>
/// <param name="Description">Description de l'anomalie, enrichie du motif à chaque transfert.</param>
/// <param name="ResolutionComment">Compte rendu saisi à la clôture ; <c>null</c> tant que l'incident n'est pas clôturé.</param>
/// <param name="AssistanceNote">Note d'assistance Markdown produite par l'analyse IA ; <c>null</c> tant qu'elle n'a pas abouti.</param>
/// <param name="IsAiProcessing">Vrai tant que l'analyse IA est en cours ; repasse à faux qu'elle réussisse ou échoue.</param>
/// <param name="AssignedByUserId">Auteur de la prise en charge (décision 0.2) ; <c>null</c> tant que le ticket n'a jamais été pris en charge.</param>
/// <param name="ClosedByUserId">Auteur de la clôture (décision 0.2) ; <c>null</c> tant que le ticket n'est pas clôturé.</param>
public record TicketResponseDto(
    Guid Id,
    Guid AssetId,
    string Title,
    string Description,
    string Criticality,
    string Status,
    Guid? AssignedTeamId,
    string AssignedTeamName,
    string? ResolutionComment,
    DateTime CreatedAt,
    string? AssistanceNote,
    bool IsAiProcessing,
    Guid? AssignedByUserId,
    Guid? ClosedByUserId);
