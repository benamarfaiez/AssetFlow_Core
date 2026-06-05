using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class TeamRepository(AssetFlowDbContext context) : ITeamRepository
{
    public async Task<Team?> GetByNameAsync(string name)
        => await context.Teams
            .FirstOrDefaultAsync(t =>
                t.Name == name.Trim() && t.IsActive);

    public async Task<Team?> GetByIdAsync(Guid id)
        => await context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IEnumerable<Team>> GetAllActiveAsync()
        => await context.Teams
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task AddAsync(Team team)
        => await context.Teams.AddAsync(team);

    public async Task<bool> ExistsWithNameAsync(string name)
        => await context.Teams
            .AnyAsync(t => t.Name == name.Trim());

    public async Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality)
        => await context.Teams
        .AsNoTracking()
        .FirstOrDefaultAsync(t =>
            t.AssetType == assetType.Trim() && t.IsActive && t.TicketCriticality == criticality.Trim());
}