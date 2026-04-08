using VTCStockManagementCase.Application.Contracts.Reports;

namespace VTCStockManagementCase.Application.Services;

public static class DailySalesAggregator
{
    public static IReadOnlyList<TopProductRow> TopNByQuantity(
        IEnumerable<(Guid ProductId, string Sku, string Name, int Qty)> rows,
        int n = 5)
    {
        return rows
            .GroupBy(x => x.ProductId)
            .Select(g =>
            {
                var first = g.First();
                return new TopProductRow
                {
                    ProductId = g.Key,
                    Sku = first.Sku,
                    Name = first.Name,
                    SoldQuantity = g.Sum(x => x.Qty)
                };
            })
            .OrderByDescending(x => x.SoldQuantity)
            .ThenBy(x => x.Sku)
            .Take(n)
            .ToList();
    }
}
