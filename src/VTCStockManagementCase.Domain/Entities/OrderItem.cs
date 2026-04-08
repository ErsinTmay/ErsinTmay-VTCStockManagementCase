namespace VTCStockManagementCase.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string SkuSnapshot { get; set; } = null!;
    public string ProductNameSnapshot { get; set; } = null!;
    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;
}
