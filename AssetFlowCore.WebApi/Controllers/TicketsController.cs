using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.WebApi.Requests;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    /// <summary>
    /// Crée un nouveau ticket de maintenance pour un actif.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TicketResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponseDto>> Create(
        [FromBody] CreateTicketRequest request,
        [FromServices] CreateMaintenanceTicketHandler handler)
    {
        var command = new CreateMaintenanceTicketCommand(request.AssetId, request.Title, request.Description, request.Criticality);
        var result = await handler.HandleAsync(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Assigne un ticket existant à un technicien.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(Guid id, [FromServices] AssignTicketToTechnicianHandler handler)
    {
        var command = new AssignTicketToTechnicianCommand(id);
        await handler.ExecuteAsync(command);
        return NoContent();
    }

    /// <summary>
    /// Clôture un ticket de maintenance avec un commentaire de résolution.
    /// </summary>
    [HttpPut("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(
        Guid id,
        [FromBody] CloseTicketRequest request,
        [FromServices] CloseTicketHandler handler)
    {
        var command = new CloseTicketCommand(id, request.ResolutionComment);
        await handler.ExecuteAsync(command);
        return NoContent();
    }
}