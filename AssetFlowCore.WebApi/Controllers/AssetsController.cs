using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.WebApi.Requests;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlowCore.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<AssetResponseDto>))]
    public async Task<ActionResult<IEnumerable<AssetResponseDto>>> GetAll([FromServices] GetAllAssetsHandler handler)
    {
        var query = new GetAllAssetsQuery();
        var result = await handler.HandleAsync(query);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AssetResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> Register(
        [FromBody] RegisterAssetRequest request,
        [FromServices] RegisterAssetHandler handler)
    {
        // Traduction de la Request HTTP en Command Applicative
        var command = new RegisterAssetCommand(request.Name, request.SerialNumber, request.Type);
        var result = await handler.HandleAsync(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}/decommission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Decommission(Guid id, [FromServices] DecommissionAssetHandler handler)
    {
        var command = new DecommissionAssetCommand(id);
        await handler.ExecuteAsync(command);
        return NoContent();
    }
}