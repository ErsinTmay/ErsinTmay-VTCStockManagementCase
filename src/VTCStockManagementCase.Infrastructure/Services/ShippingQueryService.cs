using Microsoft.EntityFrameworkCore;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Shipping;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class ShippingQueryService : IShippingService
{
    private readonly AppDbContext _db;

    public ShippingQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShippingPreparationDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var s = await _db.ShippingPreparations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (s == null) return null;
        return new ShippingPreparationDto
        {
            Id = s.Id,
            OrderId = s.OrderId,
            Status = s.Status,
            CreatedAtUtc = s.CreatedAtUtc,
            ProcessedAtUtc = s.ProcessedAtUtc,
            ErrorMessage = s.ErrorMessage
        };
    }
}
