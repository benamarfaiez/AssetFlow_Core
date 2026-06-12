using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Configuration;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlowCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration Entity Framework
        services.AddDbContext<AssetFlowDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AssetFlowDbContext).Assembly.FullName)));

        // AssetRepository (Avec gestion de cache via pattern Décorateur)
        services.AddScoped<IAssetRepository>(provider =>
        {
            var rawRepo = new AssetRepository(provider.GetRequiredService<AssetFlowDbContext>());
            return new CachedAssetRepository(rawRepo, provider.GetRequiredService<IMemoryCache>());
        });

        // TeamRepository (Avec gestion de cache via pattern Décorateur)
        services.AddScoped<ITeamRepository>(provider =>
        {
            var rawRepo = new TeamRepository(provider.GetRequiredService<AssetFlowDbContext>());
            return new CachedTeamRepository(rawRepo, provider.GetRequiredService<IMemoryCache>());
        });

        // Enregistrement du Unit of Work (Scoped)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories individuels si nécessaire
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();


        services.AddScoped<IDbContextFactory, SqlServerDbContextFactory>();

        services.AddMemoryCache();
        services.AddSignalR();
        return services;
    }
}
