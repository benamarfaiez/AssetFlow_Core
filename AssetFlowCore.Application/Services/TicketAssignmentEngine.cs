using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Services;

public class TicketAssignmentEngine : ITicketAssignmentEngine
{
    private readonly IEnumerable<IAssignmentStrategy> _strategies;

    public TicketAssignmentEngine(IEnumerable<IAssignmentStrategy> strategies)
    {
        _strategies = strategies;
    }

    public string ResolveTeam(AssetType assetType, TicketCriticality criticality)
    {
        var strategy = _strategies.FirstOrDefault(s => s.IsMatch(assetType, criticality));
        return strategy?.GetTeam() ?? "Support-Général";
    }
}
