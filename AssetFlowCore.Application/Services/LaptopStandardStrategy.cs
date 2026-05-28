using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Application.Services;

public class LaptopStandardStrategy : IAssignmentStrategy
{
    public bool IsMatch(AssetType assetType, TicketCriticality criticality) => assetType == AssetType.Laptop && criticality != TicketCriticality.High;
    public string GetTeam() => "Support-Lectorat";
}
