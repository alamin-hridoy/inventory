using InventoryPilot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPilot.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/inventory-aggregates")]
public class InventoryAggregatesController : ControllerBase
{
    private readonly InventoryApiTokenService _tokenService;
    private readonly InventoryAggregateService _aggregateService;

    public InventoryAggregatesController(InventoryApiTokenService tokenService, InventoryAggregateService aggregateService)
    {
        _tokenService = tokenService;
        _aggregateService = aggregateService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string token)
    {
        if (!_tokenService.TryReadInventoryId(token, out var inventoryId))
        {
            return Unauthorized(new { message = "Invalid inventory API token." });
        }

        var result = await _aggregateService.BuildAsync(inventoryId);
        return result is null ? NotFound() : Ok(result);
    }
}
