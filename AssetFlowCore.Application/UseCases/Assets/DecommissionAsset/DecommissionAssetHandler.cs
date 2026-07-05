using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;

public class DecommissionAssetHandler(IUnitOfWork unitOfWork) : IRequestHandler<DecommissionAssetCommand>
{
    public async Task Handle(DecommissionAssetCommand command, CancellationToken cancellationToken)
    {
        var asset = await unitOfWork.Asset.GetByIdAsync(command.Id, cancellationToken) ?? throw new DomainException($"L'actif {command.Id} est introuvable.");

        // Application stricte de la règle d'inviolabilité fonctionnelle
        int activeTickets = await unitOfWork.MaintenanceTicket.CountActiveTicketsByAssetIdAsync(command.Id);
        if (activeTickets > 0)
            throw new DomainException($"Action interdite : l'actif fait l'objet de {activeTickets} incident(s) en cours de traitement.");

        asset.Decommission();
        await unitOfWork.SaveChangesAsync(cancellationToken);

    }
}
