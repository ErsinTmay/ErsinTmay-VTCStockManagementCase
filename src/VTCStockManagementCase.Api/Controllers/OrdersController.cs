using Microsoft.AspNetCore.Mvc;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Orders;

namespace VTCStockManagementCase.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var dto = await _orders.CreateAsync(request, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _orders.GetByIdAsync(id, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost("{id:guid}/payments/simulate-success")]
    public async Task<ActionResult<OrderDto>> SimulateSuccess(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _orders.SimulatePaymentSuccessAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/payments/simulate-failure")]
    public async Task<ActionResult<OrderDto>> SimulateFailure(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _orders.SimulatePaymentFailureAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _orders.CancelAsync(id, cancellationToken);
        return Ok(dto);
    }
}
