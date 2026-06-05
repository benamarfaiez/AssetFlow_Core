using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence;

public class AssetFlowDbContext(DbContextOptions<AssetFlowDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceTicket> Tickets => Set<MaintenanceTicket>();
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetFlowDbContext).Assembly);
    }

    public async Task<int> SaveChangesAsync() => await base.SaveChangesAsync();
}