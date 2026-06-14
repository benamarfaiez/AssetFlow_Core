namespace AssetFlowCore.Domain.Repositories;

public interface IUnitOfWork
{
    // Repositories accessibles via UoW
    IAssetRepository Asset { get; }
    ITeamRepository Team { get; }
    IMaintenanceTicketRepository MaintenanceTicket { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
