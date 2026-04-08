using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Domain.Entities;

public class Order
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
    public DateTime UpdatedAtUtc { get; set; }
    public int TotalQuantity { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
