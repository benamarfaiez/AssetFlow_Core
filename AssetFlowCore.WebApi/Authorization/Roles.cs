namespace AssetFlowCore.WebApi.Authorization;

/// <summary>
/// Rôles attendus dans la revendication <c>roles</c> du jeton JWT, dérivés des quatre personas
/// du PRD (doc/PRODUCT-REQUIREMENTS.md §3). La correspondance avec les groupes d'annuaire
/// Entra ID est une tâche d'exploitation (Lot 7, étape 7.0), pas du code.
/// </summary>
public static class Roles
{
    public const string Administrateur = "Administrateur";
    public const string Technicien = "Technicien";
    public const string GestionnaireDeParc = "GestionnaireDeParc";
    public const string ResponsableEquipe = "ResponsableEquipe";
}
