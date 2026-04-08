using Microsoft.AspNetCore.Mvc;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Inventory;

namespace VTCStockManagementCase.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpPost("stock-in")]
    public async Task<ActionResult<InventoryDto>> StockIn([FromBody] StockInRequest request, CancellationToken cancellationToken)
    {
        var dto = await _inventory.StockInAsync(request, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<InventoryDto>> Get(Guid productId, CancellationToken cancellationToken)
    {
        var dto = await _inventory.GetByProductIdAsync(productId, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
    }
}
