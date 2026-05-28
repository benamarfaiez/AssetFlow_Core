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
    [HttpPost]
    public async Task<ActionResult<TicketResponseDto>> Create(
        [FromBody] CreateTicketRequest request,
        [FromServices] CreateMaintenanceTicketHandler handler)
    {
        var command = new CreateMaintenanceTicketCommand(request.AssetId, request.Title, request.Description, request.Criticality);
        var result = await handler.HandleAsync(command);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromServices] AssignTicketToTechnicianHandler handler)
    {
        var command = new AssignTicketToTechnicianCommand(id);
        await handler.ExecuteAsync(command);
        return NoContent();
    }

    [HttpPut("{id:guid}/close")]
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