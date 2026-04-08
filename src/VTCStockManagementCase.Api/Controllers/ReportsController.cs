using Microsoft.AspNetCore.Mvc;
using VTCStockManagementCase.Application.Abstractions;

namespace VTCStockManagementCase.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("daily-sales")]
    public async Task<IActionResult> DailySales([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var dto = await _reports.GetDailySalesAsync(date, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet("critical-stock")]
    public async Task<IActionResult> CriticalStock(CancellationToken cancellationToken)
    {
        return Ok(await _reports.GetCriticalStockAsync(cancellationToken));
    }
}
