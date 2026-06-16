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

    public async Task UpdateAsync(Team team)
    {
        ArgumentNullException.ThrowIfNull(team);
        // If provider supports ExecuteUpdate, prefer set-based update to avoid tracking and materialization overhead.
        var provider = context.Database.ProviderName ?? string.Empty;
        if (!provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            await context.Teams
                .Where(t => t.Id == team.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Name, _ => team.Name)
                    .SetProperty(t => t.Description, _ => team.Description)
                    .SetProperty(t => t.AssetType, _ => team.AssetType)
                    .SetProperty(t => t.TicketCriticality, _ => team.TicketCriticality)
                    .SetProperty(t => t.IsActive, _ => team.IsActive)
                );
            return;
        }

        // Fallback for providers without ExecuteUpdate (InMemory used in tests): attach + mark modified
        var entry = context.Entry(team);
        if (entry.State == EntityState.Detached)
        {
            context.Teams.Attach(team);
        }
        entry.State = EntityState.Modified;
    }

    public async Task RemoveAsync(Team team)
    {
        ArgumentNullException.ThrowIfNull(team);
        var provider = context.Database.ProviderName ?? string.Empty;
        if (!provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            await context.Teams.Where(t => t.Id == team.Id).ExecuteDeleteAsync();
            return;
        }

        // Fallback for providers without ExecuteDelete (InMemory): attach then remove
        var entry = context.Entry(team);
        if (entry.State == EntityState.Detached)
            context.Teams.Attach(team);
        context.Teams.Remove(team);
    }

    public async Task<bool> ExistsWithNameAsync(string name)
        => await context.Teams
            .AnyAsync(t => t.Name == name.Trim());

    public async Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality)
        => await context.Teams
        .AsNoTracking()
        .FirstOrDefaultAsync(t =>
            t.AssetType == assetType.Trim() && t.IsActive && t.TicketCriticality == criticality.Trim());
}