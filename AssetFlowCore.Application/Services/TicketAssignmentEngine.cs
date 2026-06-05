using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Services;

public class TicketAssignmentEngine(IEnumerable<IAssignmentStrategy> strategies) : ITicketAssignmentEngine
{
    public async Task<string> ResolveTeamIdAsync(AssetType assetType, TicketCriticality criticality)
    {
        var strategy = strategies.FirstOrDefault(s => s.IsMatch(assetType, criticality))
            ?? strategies.First(s => s is LaptopStandardStrategy); // fallback explicite

        return await strategy.GetTeamNameAsync(assetType.ToString(), criticality.ToString());
    }
}
