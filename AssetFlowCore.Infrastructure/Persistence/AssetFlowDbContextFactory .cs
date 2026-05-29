// AssetFlowCore.Infrastructure/Persistence/AssetFlowDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AssetFlowCore.Infrastructure.Persistence;

public class AssetFlowDbContextFactory : IDesignTimeDbContextFactory<AssetFlowDbContext>
{
    public AssetFlowDbContext CreateDbContext(string[] args)
    {
        // Remonte jusqu'au projet WebApi pour lire appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                         "../AssetFlowCore.WebApi"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
               "ConnectionString 'DefaultConnection' introuvable dans appsettings.json");

        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AssetFlowDbContext(options);
    }
}