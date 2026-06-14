using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AssetFlowCore.Benchmarks;

/// <summary>
/// Classe de base partagée par tous les benchmarks.
/// Configure un conteneur DI complet avec une base InMemory isolée.
/// </summary>
public abstract class BenchmarkBase
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected AssetFlowDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Appelé par BenchmarkDotNet avant chaque série de mesures [GlobalSetup].
    /// Construit le conteneur DI avec tous les services réels de l'application.
    /// </summary>
    protected void SetupServices(string dbName)
    {
        var services = new ServiceCollection();

        // Base de données InMemory isolée par benchmark
        services.AddDbContext<AssetFlowDbContext>(options =>
            options.UseInMemoryDatabase(dbName)
                   .ConfigureWarnings(w =>
                       w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                                         .InMemoryEventId.TransactionIgnoredWarning)));

        services.AddMemoryCache();

        //    Requis depuis l'ajout de IValidator<T> dans les handlers
        services.AddValidatorsFromAssemblyContaining<CreateMaintenanceTicketValidator>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // Repositories avec pattern Décorateur (Cache)
        services.AddScoped<IAssetRepository>(p =>
        {
            var raw = new AssetRepository(p.GetRequiredService<AssetFlowDbContext>());
            return new CachedAssetRepository(raw, p.GetRequiredService<IMemoryCache>());
        });
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();

        // Moteur de stratégies (Chain of Responsibility)
        services.AddScoped<IAssignmentStrategy, ServerAssignmentStrategy>();
        services.AddScoped<IAssignmentStrategy, NetworkAssignmentStrategy>();
        services.AddScoped<IAssignmentStrategy, LaptopHighCriticalityStrategy>();
        services.AddScoped<IAssignmentStrategy, LaptopStandardStrategy>();
        services.AddScoped<ITicketAssignmentEngine, TicketAssignmentEngine>();

        // Notification no-op pour les benchmarks (évite SignalR)
        services.AddScoped<INotificationService, NoOpNotificationService>();

        // Handlers applicatifs
        services.AddScoped<RegisterAssetHandler>();
        services.AddScoped<GetAllAssetsHandler>();
        services.AddScoped<DecommissionAssetHandler>();
        services.AddScoped<CreateMaintenanceTicketHandler>();
        services.AddScoped<AssignTicketToTechnicianHandler>();
        services.AddScoped<CloseTicketHandler>();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<AssetFlowDbContext>();

        // Chargement des données de référence
        SeedReferenceData();
    }

    /// <summary>Résout un service depuis un scope DI frais (simule une requête HTTP).</summary>
    protected T Resolve<T>() where T : notnull =>
        ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<T>();

    private void SeedReferenceData()
    {
        if (!DbContext.Teams.Any())
        {
            DbContext.Teams.AddRange(
                new Team("Infrastructure-Serveurs", "Server", "High", "Description de l'équipe serveurs"),
                new Team("Infrastructure-Serveurs", "Server", "Medium", "Description de l'équipe serveurs"),
                new Team("Infrastructure-Serveurs", "Server", "Low", "Description de l'équipe serveurs"),
                new Team("Support-VIP", "Laptop", "High", "Description de l'équipe support VIP"),
                new Team("Support-Lectorat", "Laptop", "Medium", "Description de l'équipe support lectorat"),
                new Team("Support-Lectorat", "Laptop", "Low", "Description de l'équipe support lectorat"),
                new Team("Réseau-Télécom", "NetworkDevice", "High", "Description de l'équipe réseaux"),
                new Team("Réseau-Télécom", "NetworkDevice", "Medium", "Description de l'équipe réseaux"),
                new Team("Réseau-Télécom", "NetworkDevice", "Low", "Description de l'équipe réseaux")
            );
            DbContext.SaveChanges();
        }
    }
}