using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality, CancellationToken cancellationToken = default);
    Task<IEnumerable<Team>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Team team, CancellationToken cancellationToken = default);
    Task RemoveAsync(Team team, CancellationToken cancellationToken = default);
}
