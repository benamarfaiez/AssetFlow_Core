using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.Application.UseCases.Team.UpdateTeam;
using AssetFlowCore.WebApi.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeam(Guid id)
    {
        var query = new GetTeamQuery(id);
        var response = await mediator.Send(query);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponseDto>> Create([FromBody] CreateTeamRequest request)
    {
        var command = new CreateTeamCommand(request.Name, request.AssetType, request.TicketCriticality, request.Description);
        var result = await mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponseDto>> Update(Guid id, [FromBody] UpdateTeamRequest request)
    {
        var command = new UpdateTeamCommand(id, request.Name, request.AssetType, request.TicketCriticality, request.Description);
        var result = await mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteTeamCommand(id);
        await mediator.Send(command);
        return NoContent();
    }
}