namespace AssetFlowCore.Application.UseCases.Team.CreateTeam;

public record CreateTeamCommand(string Name, string AssetType, string TicketCriticality, string? Description);
