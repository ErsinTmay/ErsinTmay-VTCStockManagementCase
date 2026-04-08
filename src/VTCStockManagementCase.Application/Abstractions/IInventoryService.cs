using VTCStockManagementCase.Application.Contracts.Inventory;

namespace VTCStockManagementCase.Application.Abstractions;

public interface IInventoryService
{
    Task<InventoryDto> StockInAsync(StockInRequest request, CancellationToken cancellationToken = default);
    Task<InventoryDto?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
