using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence;

public class AssetFlowDbContext : DbContext, IUnitOfWork
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceTicket> Tickets => Set<MaintenanceTicket>();

    public AssetFlowDbContext(DbContextOptions<AssetFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AssetConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceTicketConfiguration());
    }

    public async Task<int> SaveChangesAsync() => await base.SaveChangesAsync();
}