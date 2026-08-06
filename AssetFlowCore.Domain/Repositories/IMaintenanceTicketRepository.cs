using AssetFlowCore.Domain.Entities;

namespace AssetFlowCore.Domain.Repositories;

public interface IMaintenanceTicketRepository
{
    Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste une entrée d'historique de transferts, indépendamment de la navigation
    /// <see cref="MaintenanceTicket.TransferHistory"/> (non suivie par EF, voir
    /// <see cref="MaintenanceTicket.LoadTransferHistory"/>).
    /// </summary>
    Task AddTransferHistoryAsync(TicketTransferHistory entry, CancellationToken cancellationToken = default);

    /// <summary>Historique des transferts d'un ticket, du plus ancien au plus récent.</summary>
    Task<IReadOnlyCollection<TicketTransferHistory>> GetTransferHistoryAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<int> CountActiveTicketsByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
    // Return true if there exists at least one active ticket for the given asset
    // other than the ticket with id `excludingTicketId`.
    Task<bool> HasOtherActiveTicketsAsync(Guid assetId, Guid excludingTicketId, CancellationToken cancellationToken = default);
    // Return true if there exists at least one active ticket assigned to the given team
    Task<bool> ExistsActiveTicketsForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recherche paginée d'incidents, équipe assignée incluse. Le décompte total porte sur
    /// l'ensemble des incidents correspondant aux filtres, indépendamment de la pagination.
    /// </summary>
    Task<PagedResult<MaintenanceTicket>> SearchAsync(TicketSearchCriteria criteria, CancellationToken cancellationToken = default);
}
