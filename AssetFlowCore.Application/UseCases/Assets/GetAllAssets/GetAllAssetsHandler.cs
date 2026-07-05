using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Repositories;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Assets.GetAllAssets;

// Query vide pour MediatR
public record GetAllAssetsQuery : IRequest<IEnumerable<AssetResponseDto>>;

public class GetAllAssetsHandler(IAssetRepository assetRepository) : IRequestHandler<GetAllAssetsQuery, IEnumerable<AssetResponseDto>>
{
    public async Task<IEnumerable<AssetResponseDto>> Handle(GetAllAssetsQuery request, CancellationToken cancellationToken)
    {
        // Profite de l'optimisation AsNoTracking + Cache gérée par l'infrastructure
        var assets = await assetRepository.GetAllReadOnlyAsync(cancellationToken);
        return assets.Select(a => a.ToDto());
    }
}
