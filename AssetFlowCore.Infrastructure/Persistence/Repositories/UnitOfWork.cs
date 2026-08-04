using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unité de travail exposant les dépôts <b>résolus par le conteneur d'injection</b>.
/// Les instancier directement (<c>new AssetRepository(context)</c>) court-circuitait les
/// décorateurs de cache : une écriture passée par l'unité de travail n'invalidait alors
/// aucune clé et les lectures suivantes servaient un inventaire périmé pendant 5 minutes.
/// </summary>
public class UnitOfWork(
    AssetFlowDbContext context,
    IAssetRepository assetRepository,
    ITeamRepository teamRepository,
    IMaintenanceTicketRepository maintenanceTicketRepository,
    IMemoryCache memoryCache) : IUnitOfWork
{
    public ITeamRepository Team => teamRepository;

    public IAssetRepository Asset => assetRepository;

    public IMaintenanceTicketRepository MaintenanceTicket => maintenanceTicketRepository;

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Les mutations d'entités suivies (mise au rebut, passage en panne, retour en service)
        // ne traversent aucune méthode de dépôt : elles ne peuvent donc pas être détectées par
        // les décorateurs. On relève ici les listes à invalider avant que le suivi ne soit remis à zéro.
        var invalidateAssets = HasPendingChanges<Asset>();
        var invalidateTeams = HasPendingChanges<Domain.Entities.Team>();

        var affected = await context.SaveChangesAsync(cancellationToken);

        // Invalidation seulement après persistance réussie : une écriture rejetée
        // ne doit pas faire perdre un cache encore valide.
        if (invalidateAssets)
        {
            memoryCache.Remove(CacheKeys.AssetsList);
        }

        if (invalidateTeams)
        {
            memoryCache.Remove(CacheKeys.TeamsList);
            memoryCache.Remove(CacheKeys.TeamsListAll);
        }

        return affected;
    }

    private bool HasPendingChanges<TEntity>() where TEntity : class
        => context.ChangeTracker
            .Entries<TEntity>()
            .Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
}
