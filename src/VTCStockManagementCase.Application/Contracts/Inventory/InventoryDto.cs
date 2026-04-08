namespace VTCStockManagementCase.Application.Contracts.Inventory;

public class InventoryDto
{
    public Guid ProductId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
