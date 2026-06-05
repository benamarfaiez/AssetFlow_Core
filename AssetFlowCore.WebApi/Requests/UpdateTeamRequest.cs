namespace AssetFlowCore.WebApi.Requests;

public record UpdateTeamRequest(string? Name, string? AssetType, string? TicketCriticality, string? Description);
