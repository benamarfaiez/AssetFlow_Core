using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Interfaces;

public interface IAssignmentStrategy
{
    bool IsMatch(AssetType assetType, TicketCriticality criticality);
    Task<string> GetTeamNameAsync(string assetType, string criticality, CancellationToken cancellationToken = default);
}
