using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<IEnumerable<Asset>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default);
}
