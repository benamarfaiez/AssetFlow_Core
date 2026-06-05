using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class AssetRepository(AssetFlowDbContext context) : IAssetRepository
{
    public async Task<Asset?> GetByIdAsync(Guid id)
        => await context.Assets
        .Include(a => a.Tickets)
        .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<bool> ExistsWithSerialNumberAsync(string serialNumber)
        => await context.Assets
        .AnyAsync(a => a.SerialNumber.Value == serialNumber.ToUpper().Trim());

    public async Task AddAsync(Asset asset)
        => await context.Assets
        .AddAsync(asset);

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync()
        => await context.Assets
        .AsNoTracking()
        .ToListAsync();
}