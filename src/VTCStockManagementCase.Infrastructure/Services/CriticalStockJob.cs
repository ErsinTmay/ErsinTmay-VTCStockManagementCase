using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Infrastructure.Options;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class CriticalStockJob : ICriticalStockJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<CriticalStockJob> _logger;
    private readonly AppOptions _options;

    public CriticalStockJob(AppDbContext db, IOptions<AppOptions> options, ILogger<CriticalStockJob> logger)
    {
        _db = db;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var threshold = _options.CriticalStockThreshold;
        var now = DateTime.UtcNow;

        var rows = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => (x.OnHand - x.Reserved) < threshold)
            .ToListAsync(cancellationToken);

        foreach (var inv in rows)
        {
            _db.CriticalStockRecords.Add(new CriticalStockRecord
            {
                Id = Guid.NewGuid(),
                ProductId = inv.ProductId,
                Sku = inv.Product.Sku,
                AvailableStock = inv.OnHand - inv.Reserved,
                Threshold = threshold,
                DetectedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Critical stock job finished: {Count} products below threshold {Threshold}", rows.Count, threshold);
    }
}
