namespace AssetFlowCore.Application.UseCases;

public record CreateMaintenanceTicketCommand(Guid AssetId, string Title, string Description, string Criticality);
