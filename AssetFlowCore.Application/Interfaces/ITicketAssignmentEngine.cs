using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Interfaces;

public interface ITicketAssignmentEngine
{
    string ResolveTeam(AssetType assetType, TicketCriticality criticality);
}
