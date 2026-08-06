using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssetFlowCore.Application.UseCases.Assets.RestoreAssetToService;

public class RestoreAssetToServiceHandler(IUnitOfWork unitOfWork, ILogger<RestoreAssetToServiceHandler> logger) : IRequestHandler<RestoreAssetToServiceCommand>
{
    public async Task Handle(RestoreAssetToServiceCommand command, CancellationToken cancellationToken)
    {
        var asset = await unitOfWork.Asset.GetByIdAsync(command.Id, cancellationToken)
            ?? throw NotFoundException.For("L'actif", command.Id);

        asset.RestoreFromDecommission(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Traçabilité de l'opération (décision 0.4) : aucun champ dédié n'est persisté pour le
        // motif, à la différence du transfert de ticket (entité d'historique, décision 0.5).
        logger.LogInformation(
            "Actif {AssetId} remis en service depuis le rebut. Motif : {Reason}",
            command.Id,
            command.Reason);
    }
}
