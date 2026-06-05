namespace AssetFlowCore.Application.DTOs;

public record TicketResponseDto(Guid Id, Guid AssetId, string Title, string Criticality, string Status, Guid? AssignedTeamId, string AssignedTeamName);