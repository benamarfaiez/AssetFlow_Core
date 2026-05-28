using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Services;

public class ServerAssignmentStrategy : IAssignmentStrategy
{
    public bool IsMatch(AssetType assetType, TicketCriticality criticality) => assetType == AssetType.Server;
    public string GetTeam() => "Infrastructure-Serveurs";
}
