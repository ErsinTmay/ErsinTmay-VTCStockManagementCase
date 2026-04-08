using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTCStockManagementCase.Application;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Orders;
using VTCStockManagementCase.Domain;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Domain.Enums;
using VTCStockManagementCase.Domain.Exceptions;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class OrderService : IOrderService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new BusinessException(ErrorCodes.ValidationError, "Order must contain at least one item.");
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            throw new BusinessException(ErrorCodes.ValidationError, "CustomerId is required.");

        var merged = request.Items
            .Where(x => x.Quantity > 0)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (merged.Count == 0)
            throw new BusinessException(ErrorCodes.ValidationError, "Each item quantity must be greater than zero.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var productIds = merged.Select(x => x.ProductId).ToList();
        var products = await _db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var line in merged)
        {
            if (!products.TryGetValue(line.ProductId, out var p))
                throw new BusinessException(ErrorCodes.ProductNotFound, "Product not found.", new { line.ProductId });
            if (!p.IsActive)
                throw new BusinessException(ErrorCodes.ValidationError, "Product is not active.", new { line.ProductId });
            //Atomic yapıyı burada koruyoruz. Örneğin birden fazla sipariş oluşturulursa, ve birinde stok yetersiz olursa, diğer siparişler de iptal edilir.
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE "Inventories"
                 SET "Reserved" = "Reserved" + {line.Quantity}, "UpdatedAtUtc" = {now}
                 WHERE "ProductId" = {line.ProductId}
                   AND ("OnHand" - "Reserved") >= {line.Quantity}
                 """,
                cancellationToken);

            if (affected != 1)
            {
                _logger.LogWarning("Reserve failed for product {ProductId} qty {Qty}", line.ProductId, line.Quantity);
                throw new BusinessException(ErrorCodes.InsufficientStock, "Requested quantity exceeds available stock.",
                    new { productId = line.ProductId });
            }
        }

        var orderId = Guid.NewGuid();
        var orderNumber = NewOrderNumber();
        var totalQty = merged.Sum(x => x.Quantity);

        var order = new Order
        {
            Id = orderId,
            OrderNumber = orderNumber,
            CustomerId = request.CustomerId.Trim(),
            Status = OrderStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            TotalQuantity = totalQty
        };

        _db.Orders.Add(order);

        foreach (var line in merged)
        {
            var p = products[line.ProductId];
            var oi = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = line.ProductId,
                SkuSnapshot = p.Sku,
                ProductNameSnapshot = p.Name,
                Quantity = line.Quantity
            };
            _db.OrderItems.Add(oi);

            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = line.ProductId,
                OrderId = orderId,
                Type = StockMovementType.Reserve,
                Quantity = line.Quantity,
                OccurredAtUtc = now,
                Notes = null
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation("Order created {OrderId} {OrderNumber}", order.Id, order.OrderNumber);

        return await GetByIdRequiredAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return order == null ? null : Map(order);
    }

    private async Task<OrderDto> GetByIdRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.OrderNotFound, "Order not found.", new { id });
        return Map(order);
    }

    public async Task<OrderDto> SimulatePaymentSuccessAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.OrderNotFound, "Order not found.", new { orderId });

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(ErrorCodes.InvalidOrderState, "Payment simulation is only valid for Pending orders.",
                new { order.Status });

        var now = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE "Inventories"
                 SET "OnHand" = "OnHand" - {item.Quantity},
                     "Reserved" = "Reserved" - {item.Quantity},
                     "UpdatedAtUtc" = {now}
                 WHERE "ProductId" = {item.ProductId}
                   AND "Reserved" >= {item.Quantity}
                   AND "OnHand" >= {item.Quantity}
                 """,
                cancellationToken);

            if (affected != 1)
                throw new InvalidOperationException("Inventory commit invariant violated.");

            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                OrderId = order.Id,
                Type = StockMovementType.CommitSale,
                Quantity = item.Quantity,
                OccurredAtUtc = now,
                Notes = null
            });
        }

        if (!OrderStatusRules.CanTransition(order.Status, OrderStatus.Approved))
            throw new InvalidOperationException("Invalid transition from Pending to Approved.");
        order.Status = OrderStatus.Approved;
        order.ApprovedAtUtc = now;

        if (!OrderStatusRules.CanTransition(order.Status, OrderStatus.Completed))
            throw new InvalidOperationException("Invalid transition from Approved to Completed.");
        order.Status = OrderStatus.Completed;
        order.CompletedAtUtc = now;
        order.UpdatedAtUtc = now;
        //Eğer sipariş tamamlandıysa, kargo hazırlığı oluşturulur.
        //Bekleme yapmadan diğer işlemler yapılır.  
        var payload = JsonSerializer.Serialize(new { orderId = order.Id }, JsonOpts);
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = OutboxTypes.OrderCompleted,
            Payload = payload,
            OccurredAtUtc = now,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0
        });

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation("Payment success, order completed {OrderId}", order.Id);

        return await GetByIdRequiredAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDto> SimulatePaymentFailureAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.OrderNotFound, "Order not found.", new { orderId });

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(ErrorCodes.InvalidOrderState, "Payment simulation is only valid for Pending orders.",
                new { order.Status });

        var now = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE "Inventories"
                 SET "Reserved" = "Reserved" - {item.Quantity},
                     "UpdatedAtUtc" = {now}
                 WHERE "ProductId" = {item.ProductId}
                   AND "Reserved" >= {item.Quantity}
                 """,
                cancellationToken);

            if (affected != 1)
                throw new InvalidOperationException("Reserve release invariant violated.");

            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                OrderId = order.Id,
                Type = StockMovementType.ReserveRelease,
                Quantity = item.Quantity,
                OccurredAtUtc = now,
                Notes = null
            });
        }

        order.Status = OrderStatus.Failed;
        order.FailureReason = "Payment simulation failed.";
        order.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation("Payment failure, order failed {OrderId}", order.Id);

        return await GetByIdRequiredAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDto> CancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.OrderNotFound, "Order not found.", new { orderId });

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(ErrorCodes.InvalidOrderState, "Only Pending orders can be cancelled.", new { order.Status });

        var now = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE "Inventories"
                 SET "Reserved" = "Reserved" - {item.Quantity},
                     "UpdatedAtUtc" = {now}
                 WHERE "ProductId" = {item.ProductId}
                   AND "Reserved" >= {item.Quantity}
                 """,
                cancellationToken);

            if (affected != 1)
                throw new InvalidOperationException("Reserve release invariant violated.");

            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                OrderId = order.Id,
                Type = StockMovementType.ReserveRelease,
                Quantity = item.Quantity,
                OccurredAtUtc = now,
                Notes = "Cancelled"
            });
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAtUtc = now;
        order.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation("Order cancelled {OrderId}", order.Id);

        return await GetByIdRequiredAsync(order.Id, cancellationToken);
    }

    private static string NewOrderNumber() => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..24];

    private static OrderDto Map(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        CustomerId = o.CustomerId,
        Status = o.Status,
        FailureReason = o.FailureReason,
        CreatedAtUtc = o.CreatedAtUtc,
        ApprovedAtUtc = o.ApprovedAtUtc,
        CompletedAtUtc = o.CompletedAtUtc,
        CancelledAtUtc = o.CancelledAtUtc,
        TotalQuantity = o.TotalQuantity,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            SkuSnapshot = i.SkuSnapshot,
            ProductNameSnapshot = i.ProductNameSnapshot,
            Quantity = i.Quantity
        }).ToList()
    };
}
