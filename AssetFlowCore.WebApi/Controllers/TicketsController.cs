using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Application.UseCases.Tickets.GetTicket;
using AssetFlowCore.Application.UseCases.Tickets.GetTickets;
using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using AssetFlowCore.WebApi.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Liste paginée des incidents. Les filtres sont facultatifs et se cumulent.
    /// </summary>
    /// <param name="status">`Opened` · `InProgress` · `Resolved` · `Closed`.</param>
    /// <param name="criticality">`Low` · `Medium` · `High`.</param>
    /// <param name="teamId">Identifiant de l'équipe assignée.</param>
    /// <param name="assetId">Identifiant de l'actif concerné.</param>
    /// <param name="sortBy">`CreatedAt` (défaut) · `Criticality` · `Status` · `Title`.</param>
    /// <param name="sortDescending">Ordre décroissant par défaut.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResultDto<TicketResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<TicketResponseDto>>> GetTickets(
        [FromQuery] string? status,
        [FromQuery] string? criticality,
        [FromQuery] Guid? teamId,
        [FromQuery] Guid? assetId,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTicketsQuery(status, criticality, teamId, assetId, sortBy, sortDescending, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Crée un nouveau ticket de maintenance pour un actif.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TicketResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponseDto>> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateMaintenanceTicketCommand(request.AssetId, request.Title, request.Description, request.Criticality);
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute(nameof(GetTicket), new { id = result.Id }, result);
    }


    [HttpPost("{id:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferTicket(Guid id, [FromBody] TransferTicketRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestTicketTransferCommand(id, request.TargetTeam, request.Reason);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Assigne un ticket existant à un technicien.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(Guid id, CancellationToken cancellationToken)
    {
        var command = new AssignTicketToTechnicianCommand(id);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Clôture un ticket de maintenance avec un commentaire de résolution.
    /// </summary>
    [HttpPut("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseTicketRequest request, CancellationToken cancellationToken)
    {
        var command = new CloseTicketCommand(id, request.ResolutionComment);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}", Name = nameof(GetTicket))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TicketResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponseDto>> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTicketQuery(id);
        var response = await mediator.Send(query, cancellationToken);
        return Ok(response);
    }
}
