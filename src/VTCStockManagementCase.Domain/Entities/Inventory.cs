namespace VTCStockManagementCase.Domain.Entities;

public class Inventory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Product Product { get; set; } = null!;

    public int Available => OnHand - Reserved;
}
