namespace AssetFlowCore.WebApi.Requests;

public record CreateTeamRequest(string Name, string AssetType, string TicketCriticality, string? Description);
