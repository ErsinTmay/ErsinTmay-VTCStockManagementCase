using VTCStockManagementCase.Application.Contracts.Products;

namespace VTCStockManagementCase.Application.Abstractions;

public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
