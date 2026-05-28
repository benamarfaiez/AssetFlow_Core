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
    public async Task<ActionResult<IEnumerable<AssetResponseDto>>> GetAll([FromServices] GetAllAssetsHandler handler)
    {
        var query = new GetAllAssetsQuery();
        var result = await handler.HandleAsync(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AssetResponseDto>> Register(
        [FromBody] RegisterAssetRequest request,
        [FromServices] RegisterAssetHandler handler)
    {
        // Traduction de la Request HTTP en Command Applicative
        var command = new RegisterAssetCommand(request.Name, request.SerialNumber, request.Type);
        var result = await handler.HandleAsync(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/decommission")]
    public async Task<IActionResult> Decommission(Guid id, [FromServices] DecommissionAssetHandler handler)
    {
        var command = new DecommissionAssetCommand(id);
        await handler.ExecuteAsync(command);
        return NoContent();
    }
}