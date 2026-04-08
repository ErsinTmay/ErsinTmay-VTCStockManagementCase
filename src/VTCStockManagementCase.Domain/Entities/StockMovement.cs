using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? OrderId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
}
