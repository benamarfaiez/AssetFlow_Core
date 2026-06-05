using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.Services;

public class LaptopStandardStrategy(ITeamRepository teamRepository) : AssignmentStrategyBase(teamRepository)
{
    public override bool IsMatch(AssetType assetType, TicketCriticality criticality)
        => assetType == AssetType.Laptop && criticality != TicketCriticality.High;

}
