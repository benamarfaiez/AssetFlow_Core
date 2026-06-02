using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class MaintenanceTicketRepository(AssetFlowDbContext context) : IMaintenanceTicketRepository
{
    public async Task<MaintenanceTicket?> GetByIdAsync(Guid id)
        => await context.Tickets
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<MaintenanceTicket?> GetByIdWithTrackingAsync(Guid id)
        => await context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(MaintenanceTicket ticket)
        => await context.Tickets
        .AddAsync(ticket);

    public async Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId)
        => await context.Tickets
        .CountAsync(t => t.AssetId == assetId && (t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress));
}
