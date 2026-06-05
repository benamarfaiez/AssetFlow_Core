namespace AssetFlowCore.Application.UseCases.Team.UpdateTeam;

public record UpdateTeamCommand(Guid TeamId, string? Name, string? AssetType, string? TicketCriticality, string? Description);

