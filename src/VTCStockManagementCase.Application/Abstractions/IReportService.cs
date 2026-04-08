using VTCStockManagementCase.Application.Contracts.Reports;

namespace VTCStockManagementCase.Application.Abstractions;

public interface IReportService
{
    Task<DailySalesReportDto?> GetDailySalesAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<CriticalStockReportDto> GetCriticalStockAsync(CancellationToken cancellationToken = default);
}
