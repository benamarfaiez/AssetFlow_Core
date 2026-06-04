using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.WebApi.Requests;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeam(
        Guid id,
        [FromServices] GetTeamHandler handler)
    {
        var query = new GetTeamQuery(id);
        var response = await handler.ExecuteAsync(query);

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponseDto>> Create(
        [FromBody] CreateTeamRequest request,
        [FromServices] CreateTeamCommandHandler handler)
    {
        var command = new CreateTeamCommand(request.Name, request.AssetType, request.TicketCriticality, request.Description);
        var result = await handler.HandleAsync(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}