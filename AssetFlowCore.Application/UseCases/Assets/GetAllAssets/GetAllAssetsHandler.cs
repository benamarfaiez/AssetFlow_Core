using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Assets.GetAllAssets;

public class GetAllAssetsHandler
{
    private readonly IAssetRepository _assetRepository;

    public GetAllAssetsHandler(IAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<IEnumerable<AssetResponseDto>> HandleAsync(GetAllAssetsQuery query)
    {
        // Profite de l'optimisation AsNoTracking + Cache gérée par l'infrastructure
        var assets = await _assetRepository.GetAllReadOnlyAsync();
        return assets.Select(a => a.ToDto());
    }
}