using VTCStockManagementCase.Application.Contracts.Shipping;

namespace VTCStockManagementCase.Application.Abstractions;

public interface IShippingService
{
    Task<ShippingPreparationDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
