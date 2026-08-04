using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Assets.GetAsset;

public class GetAssetHandler(IAssetRepository assetRepository) : IRequestHandler<GetAssetQuery, AssetDetailResponseDto>
{
    public async Task<AssetDetailResponseDto> Handle(GetAssetQuery query, CancellationToken cancellationToken)
    {
        var asset = await assetRepository.GetByIdWithTicketsAsync(query.AssetId, cancellationToken)
            ?? throw NotFoundException.For("L'actif", query.AssetId);

        return asset.ToDetailDto();
    }
}
