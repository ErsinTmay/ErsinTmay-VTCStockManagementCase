using Microsoft.AspNetCore.Mvc;
using VTCStockManagementCase.Application.Abstractions;

namespace VTCStockManagementCase.Api.Controllers;

[ApiController]
[Route("api/shipping-preparations")]
public class ShippingPreparationsController : ControllerBase
{
    private readonly IShippingService _shipping;

    public ShippingPreparationsController(IShippingService shipping)
    {
        _shipping = shipping;
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var dto = await _shipping.GetByOrderIdAsync(orderId, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
    }
}
