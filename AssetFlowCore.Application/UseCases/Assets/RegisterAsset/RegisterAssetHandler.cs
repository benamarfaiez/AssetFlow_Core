using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Assets.RegisterAsset;

public class RegisterAssetHandler(IUnitOfWork unitOfWork) : IRequestHandler<RegisterAssetCommand, AssetResponseDto>
{
    public async Task<AssetResponseDto> Handle(RegisterAssetCommand command, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Asset.ExistsWithSerialNumberAsync(command.SerialNumber.ToUpper().Trim(), cancellationToken))
            throw new DomainException("Ce numéro de série constructeur est déjà enregistré dans le parc.");

        var serial = SerialNumber.Create(command.SerialNumber);
        var assetType = Enum.Parse<AssetType>(command.Type, ignoreCase: true);

        var asset = new Asset(Guid.NewGuid(), command.Name, serial, assetType);

        await unitOfWork.Asset.AddAsync(asset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return asset.ToDto();
    }
}