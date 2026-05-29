using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;

namespace AssetFlowCore.Application.UseCases.Assets.RegisterAsset;

public class RegisterAssetHandler
{
    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterAssetHandler(IAssetRepository assetRepository, IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AssetResponseDto> HandleAsync(RegisterAssetCommand command)
    {
        if (await _assetRepository.ExistsWithSerialNumberAsync(command.SerialNumber))
            throw new DomainException("Ce numéro de série constructeur est déjà enregistré dans le parc.");

        var serial = SerialNumber.Create(command.SerialNumber);
        var assetType = Enum.Parse<AssetType>(command.Type, ignoreCase: true);

        var asset = new Asset(Guid.NewGuid(), command.Name, serial, assetType);

        await _assetRepository.AddAsync(asset);
        await _unitOfWork.SaveChangesAsync(); // Validation via UoW

        return asset.ToDto();
    }
}