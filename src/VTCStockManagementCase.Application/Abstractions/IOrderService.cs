using VTCStockManagementCase.Application.Contracts.Orders;

namespace VTCStockManagementCase.Application.Abstractions;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> SimulatePaymentSuccessAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto> SimulatePaymentFailureAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto> CancelAsync(Guid orderId, CancellationToken cancellationToken = default);
}
