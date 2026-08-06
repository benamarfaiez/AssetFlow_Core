using AssetFlowCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence;

public class AssetFlowDbContext(DbContextOptions<AssetFlowDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceTicket> Tickets => Set<MaintenanceTicket>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetFlowDbContext).Assembly);
    }

}