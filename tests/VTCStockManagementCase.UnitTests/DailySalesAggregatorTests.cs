using VTCStockManagementCase.Application.Services;

namespace VTCStockManagementCase.UnitTests;

public class DailySalesAggregatorTests
{
    [Fact]
    public void TopNByQuantity_orders_by_sold_quantity_then_sku()
    {
        var rows = new (Guid ProductId, string Sku, string Name, int Qty)[]
        {
            (Guid.Parse("00000000-0000-0000-0000-000000000001"), "A", "A", 2),
            (Guid.Parse("00000000-0000-0000-0000-000000000002"), "B", "B", 5),
            (Guid.Parse("00000000-0000-0000-0000-000000000001"), "A", "A", 1),
            (Guid.Parse("00000000-0000-0000-0000-000000000003"), "C", "C", 5)
        };

        var top = DailySalesAggregator.TopNByQuantity(rows, 5);

        Assert.Equal(3, top.Count);
        Assert.Equal("B", top[0].Sku);
        Assert.Equal(5, top[0].SoldQuantity);
        Assert.Equal("C", top[1].Sku);
        Assert.Equal(5, top[1].SoldQuantity);
        Assert.Equal("A", top[2].Sku);
        Assert.Equal(3, top[2].SoldQuantity);
    }
}
