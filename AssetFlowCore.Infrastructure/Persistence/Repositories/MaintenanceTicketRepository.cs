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

    public async Task<PagedResult<MaintenanceTicket>> SearchAsync(TicketSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = context.Tickets
            .AsNoTracking()
            .Include(t => t.AssignedTeam)
            .AsQueryable();

        if (criteria.Status is not null)
            query = query.Where(t => t.Status == criteria.Status);

        if (criteria.Criticality is not null)
            query = query.Where(t => t.Criticality == criteria.Criticality);

        if (criteria.AssignedTeamId is not null)
            query = query.Where(t => t.AssignedTeamId == criteria.AssignedTeamId);

        if (criteria.AssetId is not null)
            query = query.Where(t => t.AssetId == criteria.AssetId);

        // Décompte avant pagination : il porte sur l'ensemble du résultat filtré.
        var totalCount = await query.CountAsync(cancellationToken);

        // Tri secondaire stable sur l'identifiant : sans lui, deux incidents partageant la
        // même valeur de tri peuvent changer de page d'un appel à l'autre.
        query = ApplySort(query, criteria).ThenBy(t => t.Id);

        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MaintenanceTicket>(items, totalCount);
    }

    // Criticité et statut sont persistés en texte (HasConversion<string>) : un tri direct sur
    // la colonne serait alphabétique (« High » < « Low » < « Medium »), donc dénué de sens
    // métier. On projette chaque valeur sur son rang, traduit en CASE côté base.
    private static readonly System.Linq.Expressions.Expression<Func<MaintenanceTicket, int>> CriticalityRank =
        t => t.Criticality == Domain.Enums.TicketCriticality.Low ? 0
           : t.Criticality == Domain.Enums.TicketCriticality.Medium ? 1
           : 2;

    private static readonly System.Linq.Expressions.Expression<Func<MaintenanceTicket, int>> StatusRank =
        t => t.Status == Domain.Enums.TicketStatus.Opened ? 0
           : t.Status == Domain.Enums.TicketStatus.InProgress ? 1
           : t.Status == Domain.Enums.TicketStatus.Resolved ? 2
           : 3;

    private static IOrderedQueryable<MaintenanceTicket> ApplySort(IQueryable<MaintenanceTicket> query, TicketSearchCriteria criteria)
        => (criteria.SortBy, criteria.SortDescending) switch
        {
            // Décroissant sur la criticité = le plus grave en tête.
            (TicketSortField.Criticality, true) => query.OrderByDescending(CriticalityRank),
            (TicketSortField.Criticality, false) => query.OrderBy(CriticalityRank),
            // Décroissant sur le statut = le plus avancé dans le cycle de vie en tête.
            (TicketSortField.Status, true) => query.OrderByDescending(StatusRank),
            (TicketSortField.Status, false) => query.OrderBy(StatusRank),
            (TicketSortField.Title, true) => query.OrderByDescending(t => t.Title),
            (TicketSortField.Title, false) => query.OrderBy(t => t.Title),
            (_, false) => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };
}
