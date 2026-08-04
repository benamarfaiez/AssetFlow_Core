using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Persistence.Repositories;

public class MaintenanceTicketRepository(AssetFlowDbContext context) : IMaintenanceTicketRepository
{
    public async Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<MaintenanceTicket?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
        => await context.Tickets
        .AddAsync(ticket, cancellationToken);

    public async Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        => await context.Tickets
        .CountAsync(t => t.AssetId == assetId && (t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress), cancellationToken);

    public async Task<bool> HasOtherActiveTicketsAsync(Guid assetId, Guid excludingTicketId, CancellationToken cancellationToken = default)
        => await context.Tickets
            .AsNoTracking()
            .Where(t => t.AssetId == assetId && t.Id != excludingTicketId)
            .AnyAsync(t => t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress, cancellationToken);

    public async Task<bool> ExistsActiveTicketsForTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
        => await context.Tickets
            .AsNoTracking()
            .AnyAsync(t => t.AssignedTeamId == teamId && (t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress), cancellationToken);
}
