using Microsoft.EntityFrameworkCore;
using VTCStockManagementCase.Domain.Entities;

namespace VTCStockManagementCase.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ShippingPreparation> ShippingPreparations => Set<ShippingPreparation>();
    public DbSet<CriticalStockRecord> CriticalStockRecords => Set<CriticalStockRecord>();
    public DbSet<DailySalesArchive> DailySalesArchives => Set<DailySalesArchive>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ürün ve stok ilişkisi burada kuruluyor.
        //Duplicate durumunda 409 hatası verilir. SKU is unique.
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(512);
            e.HasOne(x => x.Inventory).WithOne(x => x.Product).HasForeignKey<Inventory>(x => x.ProductId);
        });

        modelBuilder.Entity<Inventory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.OrderNumber).HasMaxLength(64);
            e.Property(x => x.CustomerId).HasMaxLength(128);
            e.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SkuSnapshot).HasMaxLength(128);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(512);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.OrderId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Status, x.OccurredAtUtc });
            e.Property(x => x.Type).HasMaxLength(128);
        });

        modelBuilder.Entity<ShippingPreparation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId).IsUnique();
        });

        modelBuilder.Entity<CriticalStockRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.DetectedAtUtc);
            e.Property(x => x.Sku).HasMaxLength(128);
        });

        modelBuilder.Entity<DailySalesArchive>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ReportDate).IsUnique();
        });
    }
}
