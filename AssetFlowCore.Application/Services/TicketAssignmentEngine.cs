using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Services;

public class TicketAssignmentEngine(IEnumerable<IAssignmentStrategy> strategies) : ITicketAssignmentEngine
{
    public string ResolveTeam(AssetType assetType, TicketCriticality criticality)
    {
        var strategy = strategies.FirstOrDefault(s => s.IsMatch(assetType, criticality));
        return strategy?.GetTeam() ?? "Support-Général";
    }
}
