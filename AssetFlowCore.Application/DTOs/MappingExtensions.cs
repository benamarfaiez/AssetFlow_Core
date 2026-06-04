using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Application.DTOs;

public static class MappingExtensions
{
    public static AssetResponseDto ToDto(this Asset asset) =>
        new(asset.Id, asset.Name, asset.SerialNumber.Value, asset.Type.ToString(), asset.Status.ToString(), asset.CreatedAt);

    public static TicketResponseDto ToDto(this MaintenanceTicket ticket, string teamName) =>
        new(ticket.Id, ticket.AssetId, ticket.Title, ticket.Criticality.ToString(), ticket.Status.ToString(), ticket.AssignedTeamId, teamName);

    public static TeamResponseDto ToDto(this Team team) =>
        new(team.Id, team.Name, team.Description, team.IsActive, team.CreatedAt);

}