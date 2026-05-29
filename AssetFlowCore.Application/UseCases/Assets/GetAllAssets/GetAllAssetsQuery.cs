using AssetFlowCore.Application.DTOs;
using System.Collections.Generic;

namespace AssetFlowCore.Application.UseCases.Assets.GetAllAssets;

/// <summary>
/// Requête immuable représentant l'intention de lire l'intégralité du parc d'actifs.
/// En CQRS, les Queries portent les critères de filtrage (ici aucun paramètre car on liste tout).
/// </summary>
public record GetAllAssetsQuery();