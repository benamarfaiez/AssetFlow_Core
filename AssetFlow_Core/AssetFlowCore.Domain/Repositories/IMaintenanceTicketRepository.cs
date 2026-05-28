using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface IMaintenanceTicketRepository
{
    Task<MaintenanceTicket?> GetByIdAsync(Guid id);
    Task AddAsync(MaintenanceTicket ticket);
    Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId);
}
