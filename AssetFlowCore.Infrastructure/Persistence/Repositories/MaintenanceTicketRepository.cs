using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class MaintenanceTicketRepository(AssetFlowDbContext context) : IMaintenanceTicketRepository
{
    public async Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<MaintenanceTicket?> GetByIdWithTrackingAsync(Guid id)
        => await context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(MaintenanceTicket ticket)
        => await context.Tickets
        .AddAsync(ticket);

    public async Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId)
        => await context.Tickets
        .CountAsync(t => t.AssetId == assetId && (t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress));

    public async Task<bool> HasOtherActiveTicketsAsync(Guid assetId, Guid excludingTicketId)
        => await context.Tickets
            .AsNoTracking()
            .Where(t => t.AssetId == assetId && t.Id != excludingTicketId)
            .AnyAsync(t => t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress);

    public async Task<bool> ExistsActiveTicketsForTeamAsync(Guid teamId)
        => await context.Tickets
            .AsNoTracking()
            .AnyAsync(t => t.AssignedTeamId == teamId && (t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress));
}
