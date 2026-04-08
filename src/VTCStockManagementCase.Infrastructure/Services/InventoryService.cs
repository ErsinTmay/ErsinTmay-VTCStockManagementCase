using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTCStockManagementCase.Application;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Inventory;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Domain.Enums;
using VTCStockManagementCase.Domain.Exceptions;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(AppDbContext db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InventoryDto> StockInAsync(StockInRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new BusinessException(ErrorCodes.ValidationError, "Quantity must be greater than zero.");

        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.ProductNotFound, "Product not found.", new { request.ProductId });

        var inv = await _db.Inventories.FirstOrDefaultAsync(x => x.ProductId == product.Id, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.ValidationError, "Inventory row missing for product.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        inv.OnHand += request.Quantity;
        inv.UpdatedAtUtc = now;
        product.UpdatedAtUtc = now;

        _db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            OrderId = null,
            Type = StockMovementType.StockIncrease,
            Quantity = request.Quantity,
            OccurredAtUtc = now,
            Notes = request.Reason
        });

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation("Stock-in {ProductId} +{Qty}", product.Id, request.Quantity);

        return new InventoryDto
        {
            ProductId = inv.ProductId,
            OnHand = inv.OnHand,
            Reserved = inv.Reserved,
            Available = inv.OnHand - inv.Reserved,
            UpdatedAtUtc = inv.UpdatedAtUtc
        };
    }

    public async Task<InventoryDto?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var inv = await _db.Inventories.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (inv == null) return null;
        return new InventoryDto
        {
            ProductId = inv.ProductId,
            OnHand = inv.OnHand,
            Reserved = inv.Reserved,
            Available = inv.OnHand - inv.Reserved,
            UpdatedAtUtc = inv.UpdatedAtUtc
        };
    }
}
