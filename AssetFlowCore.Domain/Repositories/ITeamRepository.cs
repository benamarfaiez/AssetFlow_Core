using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByNameAsync(string name);
    Task<Team?> GetByIdAsync(Guid id);
    Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality);
    Task<IEnumerable<Team>> GetAllActiveAsync();
    Task AddAsync(Team team);
    Task<bool> ExistsWithNameAsync(string name);
    Task RemoveAsync(Team team);
}