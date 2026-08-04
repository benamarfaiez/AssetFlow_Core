namespace AssetFlowCore.Infrastructure.Cache;

/// <summary>
/// Clés de cache partagées entre les décorateurs de dépôts et l'unité de travail.
/// Centralisées ici afin qu'une écriture puisse invalider une liste mise en cache
/// par un décorateur sans dupliquer la chaîne de caractères.
/// </summary>
public static class CacheKeys
{
    /// <summary>Inventaire complet des actifs (lecture seule).</summary>
    public const string AssetsList = "Assets_List_ReadOnly";

    /// <summary>Liste des équipes actives.</summary>
    public const string TeamsList = "Teams_List_Active";

    /// <summary>Entrée unitaire d'une équipe.</summary>
    public static string Team(Guid id) => $"team_{id:N}";
}
