using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Services;

public class NetworkAssignmentStrategy : IAssignmentStrategy
{
    public bool IsMatch(AssetType assetType, TicketCriticality criticality) => assetType == AssetType.NetworkDevice;
    public string GetTeam() => "Réseau-Télécom";
}
