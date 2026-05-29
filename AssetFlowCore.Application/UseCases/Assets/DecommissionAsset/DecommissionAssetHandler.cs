using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;

public class DecommissionAssetHandler
{
    private readonly IAssetRepository _assetRepository;
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DecommissionAssetHandler(IAssetRepository assetRepository, IMaintenanceTicketRepository ticketRepository, IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(DecommissionAssetCommand command)
    {
        var asset = await _assetRepository.GetByIdAsync(command.Id);
        if (asset == null) throw new DomainException($"L'actif {command.Id} est introuvable.");

        // Application stricte de la règle d'inviolabilité fonctionnelle
        int activeTickets = await _ticketRepository.CountActiveTicketsByAssetIdAsync(command.Id);
        if (activeTickets > 0)
            throw new DomainException($"Action interdite : l'actif fait l'objet de {activeTickets} incident(s) en cours de traitement.");

        asset.Decommission();
        await _unitOfWork.SaveChangesAsync();
    }
}