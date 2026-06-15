using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;


public class UnitOfWork(AssetFlowDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    // Lazy loading des repositories
    private ITeamRepository? _teamRepository;
    private IAssetRepository? _assetRepository;
    private IMaintenanceTicketRepository? _maintenanceTicketRepository;

    // Création à la demande (lazy initialization)
    public ITeamRepository Team =>
        _teamRepository ??= new TeamRepository(context);

    public IAssetRepository Asset =>
        _assetRepository ??= new AssetRepository(context);

    public IMaintenanceTicketRepository MaintenanceTicket =>
    _maintenanceTicketRepository ??= new MaintenanceTicketRepository(context);

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Aucune transaction active.");

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Aucune transaction active.");

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}