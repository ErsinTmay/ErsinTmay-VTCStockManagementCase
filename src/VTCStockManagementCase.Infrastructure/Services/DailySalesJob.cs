using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Reports;
using VTCStockManagementCase.Application.Services;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Domain.Enums;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class DailySalesJob : IDailySalesJob
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly AppDbContext _db;
    private readonly ILogger<DailySalesJob> _logger;

    public DailySalesJob(AppDbContext db, ILogger<DailySalesJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunForPreviousUtcDayAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var reportDate = DateOnly.FromDateTime(utcNow.Date.AddDays(-1));
        var dayStart = reportDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var orders = await _db.Orders.AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.Completed
                        && x.CompletedAtUtc >= dayStart
                        && x.CompletedAtUtc < dayEnd)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var lines = new List<(Guid ProductId, string Sku, string Name, int Qty)>();

        foreach (var o in orders)
        {
            foreach (var i in o.Items)
            {
                lines.Add((i.ProductId, i.SkuSnapshot, i.ProductNameSnapshot, i.Quantity));
            }
        }

        var totalSold = lines.Sum(x => x.Qty);
        var top = DailySalesAggregator.TopNByQuantity(lines, 5);
        var json = JsonSerializer.Serialize(top, JsonOpts);

        var existing = await _db.DailySalesArchives.FirstOrDefaultAsync(x => x.ReportDate == reportDate, cancellationToken);
        var generated = DateTime.UtcNow;

        if (existing == null)
        {
            _db.DailySalesArchives.Add(new DailySalesArchive
            {
                Id = Guid.NewGuid(),
                ReportDate = reportDate,
                TotalCompletedOrders = totalOrders,
                TotalSoldQuantity = totalSold,
                GeneratedAtUtc = generated,
                TopProductsJson = json
            });
        }
        else
        {
            existing.TotalCompletedOrders = totalOrders;
            existing.TotalSoldQuantity = totalSold;
            existing.GeneratedAtUtc = generated;
            existing.TopProductsJson = json;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Daily sales archive for {ReportDate}: orders={Orders}, soldQty={SoldQty}",
            reportDate, totalOrders, totalSold);
    }
}
