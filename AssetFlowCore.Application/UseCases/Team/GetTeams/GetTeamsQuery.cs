using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Team.GetTeams;

/// <summary>
/// Liste des équipes, triée par nom.
/// </summary>
/// <param name="OnlyActive">
/// Vrai pour ne retenir que les équipes actives — celles susceptibles de recevoir un incident.
/// Faux (défaut) pour l'écran d'administration, qui doit voir aussi les équipes désactivées.
/// </param>
public record GetTeamsQuery(bool OnlyActive = false) : IRequest<IReadOnlyCollection<TeamResponseDto>>;
