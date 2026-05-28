using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.Infrastructure.Repositories;

public class MaintenanceTicketRepository : IMaintenanceTicketRepository
{
    private readonly AssetFlowDbContext _context;
    public MaintenanceTicketRepository(AssetFlowDbContext context) => _context = context;

    public async Task<MaintenanceTicket?> GetByIdAsync(Guid id) => await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
    public async Task AddAsync(MaintenanceTicket ticket) => await _context.Tickets.AddAsync(ticket);
    public async Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId) =>
        await _context.Tickets.CountAsync(t => t.AssetId == assetId && (t.Status == Domain.Enums.TicketStatus.Opened || t.Status == Domain.Enums.TicketStatus.InProgress));
}
