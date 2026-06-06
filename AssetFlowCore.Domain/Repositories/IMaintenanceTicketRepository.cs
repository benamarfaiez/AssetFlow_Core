using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface IMaintenanceTicketRepository
{
    Task<MaintenanceTicket?> GetByIdAsync(Guid id);
    Task<MaintenanceTicket?> GetByIdWithTrackingAsync(Guid id);
    Task AddAsync(MaintenanceTicket ticket);
    Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId);
    // Return true if there exists at least one active ticket for the given asset
    // other than the ticket with id `excludingTicketId`.
    Task<bool> HasOtherActiveTicketsAsync(Guid assetId, Guid excludingTicketId);
    // Return true if there exists at least one active ticket assigned to the given team
    Task<bool> ExistsActiveTicketsForTeamAsync(Guid teamId);
}
