using Microsoft.EntityFrameworkCore;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Domain.Enums;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class OutboxMetricsService : IOutboxMetrics
{
    private readonly AppDbContext _db;

    public OutboxMetricsService(AppDbContext db)
    {
        _db = db;
    }

    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return _db.OutboxMessages.CountAsync(x => x.Status == OutboxMessageStatus.Pending, cancellationToken);
    }
}
