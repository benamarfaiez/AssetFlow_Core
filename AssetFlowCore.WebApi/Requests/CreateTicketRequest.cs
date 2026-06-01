namespace AssetFlowCore.WebApi.Requests;

public record CreateTicketRequest(Guid AssetId, string Title, string Description, string Criticality);