using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Assets.GetAsset;

/// <summary>Fiche d'un actif et de ses incidents.</summary>
public record GetAssetQuery(Guid AssetId) : IRequest<AssetDetailResponseDto>;
