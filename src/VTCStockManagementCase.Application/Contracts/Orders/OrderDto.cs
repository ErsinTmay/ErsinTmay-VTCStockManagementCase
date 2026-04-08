using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Application.Contracts.Orders;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public int TotalQuantity { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string SkuSnapshot { get; set; } = null!;
    public string ProductNameSnapshot { get; set; } = null!;
    public int Quantity { get; set; }
}
