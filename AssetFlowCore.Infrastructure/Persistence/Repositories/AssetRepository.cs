using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class AssetRepository(AssetFlowDbContext context) : IAssetRepository
{
    public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Assets
        .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<bool> ExistsWithSerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        => await context.Assets
        .AnyAsync(a => a.SerialNumber.Value == serialNumber, cancellationToken);

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
        => await context.Assets
        .AddAsync(asset, cancellationToken);

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default)
        => await context.Assets
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}