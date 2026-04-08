using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTCStockManagementCase.Application;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Products;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Domain.Exceptions;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new BusinessException(ErrorCodes.ValidationError, "SKU is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException(ErrorCodes.ValidationError, "Product name is required.");

        var sku = request.Sku.Trim();
        if (await _db.Products.AnyAsync(x => x.Sku == sku, cancellationToken))
            throw new BusinessException(ErrorCodes.DuplicateSku, "SKU already exists.", new { sku });

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            OnHand = 0,
            Reserved = 0,
            UpdatedAtUtc = now
        };

        _db.Products.Add(product);
        _db.Inventories.Add(inventory);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created {ProductId} {Sku}", product.Id, product.Sku);
        return Map(product);
    }

    public async Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Products.AsNoTracking().OrderBy(x => x.Sku).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return p == null ? null : Map(p);
    }

    private static ProductDto Map(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Description = p.Description,
        IsActive = p.IsActive,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc
    };
}
