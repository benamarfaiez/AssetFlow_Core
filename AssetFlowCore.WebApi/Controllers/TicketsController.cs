using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Application.UseCases.Tickets.GetTicket;
using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using AssetFlowCore.WebApi.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Crée un nouveau ticket de maintenance pour un actif.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TicketResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponseDto>> Create([FromBody] CreateTicketRequest request)
    {
        var command = new CreateMaintenanceTicketCommand(request.AssetId, request.Title, request.Description, request.Criticality);
        var result = await mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }


    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> TransferTicket(Guid id, [FromBody] TransferTicketRequest request)
    {
        var command = new RequestTicketTransferCommand(id, request.TargetTeam, request.Reason);
        await mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Assigne un ticket existant à un technicien.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(Guid id)
    {
        var command = new AssignTicketToTechnicianCommand(id);
        await mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Clôture un ticket de maintenance avec un commentaire de résolution.
    /// </summary>
    [HttpPut("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseTicketRequest request)
    {
        var command = new CloseTicketCommand(id, request.ResolutionComment);
        await mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TicketResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var query = new GetTicketQuery(id);
        var response = await mediator.Send(query);
        return Ok(response);
    }
}