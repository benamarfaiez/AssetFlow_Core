using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.GetAsset;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.WebApi.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<AssetResponseDto>))]
    public async Task<ActionResult<IEnumerable<AssetResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllAssetsQuery();
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Fiche d'un actif : ses caractéristiques et l'ensemble de ses incidents.
    /// </summary>
    [HttpGet("{id:guid}", Name = nameof(GetAsset))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AssetDetailResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDetailResponseDto>> GetAsset(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAssetQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AssetResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> Register([FromBody] RegisterAssetRequest request, CancellationToken cancellationToken)
    {
        // Traduction de la Request HTTP en Command Applicative
        var command = new RegisterAssetCommand(request.Name, request.SerialNumber, request.Type);
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute(nameof(GetAsset), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/decommission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Decommission(Guid id, CancellationToken cancellationToken)
    {
        var command = new DecommissionAssetCommand(id);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
