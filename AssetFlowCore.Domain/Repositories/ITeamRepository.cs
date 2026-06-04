using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.Domain.Repositories;

public interface ITeamRepository
{
    /// <summary>Retourne une équipe par son nom exact (insensible à la casse).</summary>
    Task<Team?> GetByNameAsync(string name);

    /// <summary>Retourne une équipe par son identifiant.</summary>
    Task<Team?> GetByIdAsync(Guid id);
    Task<Team?> GetByAssetTypeAndCriticalityAsync(string assetType, string criticality);
    
    /// <summary>Retourne toutes les équipes actives.</summary>
    Task<IEnumerable<Team>> GetAllActiveAsync();

    /// <summary>Ajoute une nouvelle équipe.</summary>
    Task AddAsync(Team team);

    /// <summary>Vérifie si une équipe existe déjà avec ce nom.</summary>
    Task<bool> ExistsWithNameAsync(string name);
}