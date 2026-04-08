using Microsoft.AspNetCore.Mvc;
using VTCStockManagementCase.Application.Abstractions;

namespace VTCStockManagementCase.Api.Controllers;

[ApiController]
[Route("api/outbox")]
public class OutboxController : ControllerBase
{
    private readonly IOutboxMetrics _metrics;

    public OutboxController(IOutboxMetrics metrics)
    {
        _metrics = metrics;
    }

    [HttpGet("pending-count")]
    public async Task<ActionResult<object>> PendingCount(CancellationToken cancellationToken)
    {
        var count = await _metrics.GetPendingCountAsync(cancellationToken);
        return Ok(new { pendingCount = count });
    }
}
