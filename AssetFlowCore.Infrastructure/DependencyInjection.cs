using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Configuration;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlowCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Outils techniques requis par les services
        services.AddMemoryCache();
        services.AddSignalR();

        // 2. Configuration du DbContext & Options
        services.AddDbContext<AssetFlowDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AssetFlowDbContext).Assembly.GetName().Name)));

        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName);

        // 3. AssetRepository (Pattern Décorateur propre via IoC)
        services.AddScoped<AssetRepository>();
        services.AddScoped<IAssetRepository>(provider =>
            new CachedAssetRepository(
                provider.GetRequiredService<AssetRepository>(),
                provider.GetRequiredService<IMemoryCache>()
            ));

        // 4. TeamRepository
        services.AddScoped<TeamRepository>();
        services.AddScoped<ITeamRepository>(provider =>
            new CachedTeamRepository(
                provider.GetRequiredService<TeamRepository>(),
                provider.GetRequiredService<IMemoryCache>()
            ));

        // 5. Autres repositories et Unité de Travail
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 6. Services d'infrastructure transverses
        services.AddScoped<IDbContextFactory, SqlServerDbContextFactory>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        return services;
    }
}