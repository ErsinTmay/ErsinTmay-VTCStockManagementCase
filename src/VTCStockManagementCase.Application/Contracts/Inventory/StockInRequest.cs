namespace VTCStockManagementCase.Application.Contracts.Inventory;

public class StockInRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
