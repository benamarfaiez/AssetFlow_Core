using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.WebApi.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<AssetResponseDto>))]
    public async Task<ActionResult<IEnumerable<AssetResponseDto>>> GetAll()
    {
        var query = new GetAllAssetsQuery();
        var result = await mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AssetResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> Register([FromBody] RegisterAssetRequest request)
    {
        // Traduction de la Request HTTP en Command Applicative
        var command = new RegisterAssetCommand(request.Name, request.SerialNumber, request.Type);
        var result = await mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}/decommission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Decommission(Guid id)
    {
        var command = new DecommissionAssetCommand(id);
        await mediator.Send(command);
        return NoContent();
    }
}