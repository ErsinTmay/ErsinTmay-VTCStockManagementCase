namespace VTCStockManagementCase.Application.Contracts.Orders;

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = null!;
    public List<OrderLineRequest> Items { get; set; } = new();
}

public class OrderLineRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
