using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;

public class DecommissionAssetHandler(IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(DecommissionAssetCommand command)
    {
        var asset = await unitOfWork.Asset.GetByIdAsync(command.Id) ?? throw new DomainException($"L'actif {command.Id} est introuvable.");

        // Application stricte de la règle d'inviolabilité fonctionnelle
        int activeTickets = await unitOfWork.MaintenanceTicket.CountActiveTicketsByAssetIdAsync(command.Id);
        if (activeTickets > 0)
            throw new DomainException($"Action interdite : l'actif fait l'objet de {activeTickets} incident(s) en cours de traitement.");

        asset.Decommission();
        await unitOfWork.SaveChangesAsync();
    }
}