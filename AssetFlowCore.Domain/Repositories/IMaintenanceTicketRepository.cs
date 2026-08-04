using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface IMaintenanceTicketRepository
{
    Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
    // Return true if there exists at least one active ticket for the given asset
    // other than the ticket with id `excludingTicketId`.
    Task<bool> HasOtherActiveTicketsAsync(Guid assetId, Guid excludingTicketId, CancellationToken cancellationToken = default);
    // Return true if there exists at least one active ticket assigned to the given team
    Task<bool> ExistsActiveTicketsForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
}
