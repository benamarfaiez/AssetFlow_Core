using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.Services;

public abstract class AssignmentStrategyBase(ITeamRepository teamRepository) : IAssignmentStrategy
{
    public abstract bool IsMatch(AssetType assetType, TicketCriticality criticality);

    // Résolution de l'Id depuis la base — centralisée ici une seule fois
    public async Task<string> GetTeamNameAsync(string assetType, string criticality)
    {
        var team = await teamRepository.GetByAssetTypeAndCriticalityAsync(assetType, criticality)
            ?? throw new DomainException(
                $"L'équipe est introuvable en base. " +
                "Vérifiez que les données de référence sont à jour.");

        return team.Name;
    }

}