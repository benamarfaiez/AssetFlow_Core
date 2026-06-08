using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class AssetRepository(AssetFlowDbContext context) : IAssetRepository
{
    public async Task<Asset?> GetByIdAsync(Guid id)
        => await context.Assets
        .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<bool> ExistsWithSerialNumberAsync(string serialNumber)
        => await context.Assets
        .AnyAsync(a => a.SerialNumber.Value == serialNumber);

    public async Task AddAsync(Asset asset)
        => await context.Assets
        .AddAsync(asset);

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync()
        => await context.Assets
        .AsNoTracking()
        .ToListAsync();
}