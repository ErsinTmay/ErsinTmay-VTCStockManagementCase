using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Domain.Enums;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext db, ILogger<DbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var catalog = new[]
        {
            new { Sku = "SKU-SEED-001", Name = "Gaming Mouse", Description = "Wireless gaming mouse", OnHand = 120 },
            new { Sku = "SKU-SEED-002", Name = "Mechanical Keyboard", Description = "Blue switch keyboard", OnHand = 95 },
            new { Sku = "SKU-SEED-003", Name = "USB-C Dock", Description = "8-in-1 docking station", OnHand = 32 },
            new { Sku = "SKU-SEED-004", Name = "Laptop Stand", Description = "Aluminum stand", OnHand = 14 },
            new { Sku = "SKU-SEED-005", Name = "Webcam 1080p", Description = "Auto-focus webcam", OnHand = 7 }
        };

        foreach (var item in catalog)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.Sku == item.Sku, cancellationToken);
            if (product == null)
            {
                product = new Product
                {
                    Id = Guid.NewGuid(),
                    Sku = item.Sku,
                    Name = item.Name,
                    Description = item.Description,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                _db.Products.Add(product);
                _db.Inventories.Add(new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    OnHand = item.OnHand,
                    Reserved = 0,
                    UpdatedAtUtc = now
                });
                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Type = StockMovementType.StockIncrease,
                    Quantity = item.OnHand,
                    OccurredAtUtc = now,
                    Notes = "Startup seed"
                });
            }
        }

        // Persist newly inserted products/inventory before querying them for seed orders.
        await _db.SaveChangesAsync(cancellationToken);

        // Seed one completed order for report queries if missing.
        if (!await _db.Orders.AnyAsync(x => x.OrderNumber == "SEED-ORDER-001", cancellationToken))
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.Sku == "SKU-SEED-001", cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Seed product SKU-SEED-001 not found; skipping seed order");
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Database seed completed");
                return;
            }

            var inventory = await _db.Inventories.FirstOrDefaultAsync(x => x.ProductId == product.Id, cancellationToken);
            if (inventory == null)
            {
                _logger.LogWarning("Seed inventory for SKU-SEED-001 not found; skipping seed order");
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Database seed completed");
                return;
            }

            if (inventory.OnHand >= 2)
            {
                var completedAt = now.AddHours(-2);
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "SEED-ORDER-001",
                    CustomerId = "seed-customer-001",
                    Status = OrderStatus.Completed,
                    CreatedAtUtc = completedAt.AddMinutes(-5),
                    ApprovedAtUtc = completedAt.AddMinutes(-1),
                    CompletedAtUtc = completedAt,
                    UpdatedAtUtc = completedAt,
                    TotalQuantity = 2
                };

                _db.Orders.Add(order);
                _db.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    SkuSnapshot = product.Sku,
                    ProductNameSnapshot = product.Name,
                    Quantity = 2
                });

                inventory.OnHand -= 2;
                inventory.UpdatedAtUtc = now;

                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    OrderId = order.Id,
                    Type = StockMovementType.CommitSale,
                    Quantity = 2,
                    OccurredAtUtc = completedAt,
                    Notes = "Startup seed completed order"
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Database seed completed");
    }
}
