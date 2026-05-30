using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly AssetFlowDbContext _context;
    public AssetRepository(AssetFlowDbContext context) => _context = context;

    public async Task<Asset?> GetByIdAsync(Guid id) 
        => await _context.Assets
        .AsNoTracking()
        .Include(a => a.Tickets)
        .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<bool> ExistsWithSerialNumberAsync(string serialNumber) 
        => await _context.Assets
        .AnyAsync(a => a.SerialNumber.Value == serialNumber.ToUpper().Trim());

    public async Task AddAsync(Asset asset) 
        => await _context.Assets
        .AddAsync(asset);

    public async Task<IEnumerable<Asset>> GetAllReadOnlyAsync() 
        => await _context.Assets
        .AsNoTracking()
        .ToListAsync();
}