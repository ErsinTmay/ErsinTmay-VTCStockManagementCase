using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Reports;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class ReportService : IReportService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DailySalesReportDto?> GetDailySalesAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var row = await _db.DailySalesArchives.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReportDate == date, cancellationToken);
        if (row == null) return null;

        var top = JsonSerializer.Deserialize<List<TopProductRow>>(row.TopProductsJson, JsonOpts) ?? new List<TopProductRow>();

        return new DailySalesReportDto
        {
            ReportDate = row.ReportDate,
            TotalCompletedOrders = row.TotalCompletedOrders,
            TotalSoldQuantity = row.TotalSoldQuantity,
            GeneratedAtUtc = row.GeneratedAtUtc,
            TopProducts = top
        };
    }

    public async Task<CriticalStockReportDto> GetCriticalStockAsync(CancellationToken cancellationToken = default)
    {
        var all = await _db.CriticalStockRecords.AsNoTracking()
            .OrderByDescending(x => x.DetectedAtUtc)
            .ToListAsync(cancellationToken);

        var latest = all
            .GroupBy(x => x.ProductId)
            .Select(g => g.OrderByDescending(x => x.DetectedAtUtc).First())
            .OrderBy(x => x.Sku)
            .Select(x => new CriticalStockRow
            {
                ProductId = x.ProductId,
                Sku = x.Sku,
                AvailableStock = x.AvailableStock,
                Threshold = x.Threshold,
                DetectedAtUtc = x.DetectedAtUtc
            })
            .ToList();

        return new CriticalStockReportDto { Items = latest };
    }
}
