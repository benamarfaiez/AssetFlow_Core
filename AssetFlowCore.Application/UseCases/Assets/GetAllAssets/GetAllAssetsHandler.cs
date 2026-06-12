using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases.Assets.GetAllAssets;

public class GetAllAssetsHandler(IAssetRepository assetRepository)
{
    public async Task<IEnumerable<AssetResponseDto>> HandleAsync()
    {
        // Profite de l'optimisation AsNoTracking + Cache gérée par l'infrastructure
        var assets = await assetRepository.GetAllReadOnlyAsync();
        return assets.Select(a => a.ToDto());
    }
}