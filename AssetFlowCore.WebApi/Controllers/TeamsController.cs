using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.ActivateTeam;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Application.UseCases.Team.DeactivateTeam;
using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeams;
using AssetFlowCore.Application.UseCases.Team.UpdateTeam;
using AssetFlowCore.WebApi.Authorization;
using AssetFlowCore.WebApi.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TeamsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Liste des équipes, triée par nom.
    /// </summary>
    /// <param name="onlyActive">Vrai pour ne retenir que les équipes actives.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<TeamResponseDto>))]
    public async Task<ActionResult<IReadOnlyCollection<TeamResponseDto>>> GetTeams(
        [FromQuery] bool onlyActive,
        CancellationToken cancellationToken)
    {
        var query = new GetTeamsQuery(onlyActive);
        var response = await mediator.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}", Name = nameof(GetTeam))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponseDto>> GetTeam(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTeamQuery(id);
        var response = await mediator.Send(query, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Réservée à l'administrateur du référentiel (PRD §3) : création et maintenance des équipes.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Administrateur)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponseDto>> Create([FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTeamCommand(request.Name, request.AssetType, request.TicketCriticality, request.Description);
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute(nameof(GetTeam), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Administrateur)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponseDto>> Update(Guid id, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTeamCommand(id, request.Name, request.AssetType, request.TicketCriticality, request.Description);
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Administrateur)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteTeamCommand(id);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Réactive une équipe désactivée : redevient éligible à <c>?onlyActive=true</c> et aux
    /// nouvelles assignations (décision 0.6).
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [Authorize(Roles = Roles.Administrateur)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponseDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var command = new ActivateTeamCommand(id);
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Désactive une équipe : disparaît de <c>?onlyActive=true</c> et cesse de recevoir de
    /// nouveaux incidents, sans être supprimée (décision 0.6).
    /// </summary>
    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = Roles.Administrateur)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponseDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeactivateTeamCommand(id);
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
