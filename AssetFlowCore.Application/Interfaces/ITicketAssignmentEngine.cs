using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Interfaces;

public interface ITicketAssignmentEngine
{
    Task<string> ResolveTeamIdAsync(AssetType assetType, TicketCriticality criticality);
}
