using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Application.DTOs;

public static class MappingExtensions
{
    public static AssetResponseDto ToDto(this Asset asset) =>
        new(asset.Id, asset.Name, asset.SerialNumber.Value, asset.Type.ToString(), asset.Status.ToString(), asset.CreatedAt);

    /// <summary>
    /// Fiche détaillée d'un actif. Les incidents doivent avoir été chargés avec leur équipe
    /// (<c>Include</c>) : le mapping ne déclenche aucune requête supplémentaire.
    /// </summary>
    public static AssetDetailResponseDto ToDetailDto(this Asset asset) =>
        new(asset.Id,
            asset.Name,
            asset.SerialNumber.Value,
            asset.Type.ToString(),
            asset.Status.ToString(),
            asset.CreatedAt,
            [.. asset.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new AssetTicketDto(
                    t.Id,
                    t.Title,
                    t.Criticality.ToString(),
                    t.Status.ToString(),
                    t.CreatedAt,
                    t.AssignedTeamId,
                    t.AssignedTeam?.Name ?? string.Empty))]);

    public static TicketResponseDto ToDto(this MaintenanceTicket ticket, string teamName) =>
        new(ticket.Id,
            ticket.AssetId,
            ticket.Title,
            ticket.Description,
            ticket.Criticality.ToString(),
            ticket.Status.ToString(),
            ticket.AssignedTeamId,
            teamName,
            ticket.ResolutionComment,
            ticket.CreatedAt,
            ticket.AssistanceNote,
            ticket.IsAiProcessing);

    public static TeamResponseDto ToDto(this Team team) =>
        new(team.Id, team.Name, team.Description, team.IsActive, team.CreatedAt, team.AssetType, team.TicketCriticality);

}
