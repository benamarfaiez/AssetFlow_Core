using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id);
    Task<bool> ExistsWithSerialNumberAsync(string serialNumber);
    Task AddAsync(Asset asset);
    Task<IEnumerable<Asset>> GetAllReadOnlyAsync();
}
