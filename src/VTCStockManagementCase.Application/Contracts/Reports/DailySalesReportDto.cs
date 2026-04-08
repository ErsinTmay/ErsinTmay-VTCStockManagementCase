namespace VTCStockManagementCase.Application.Contracts.Reports;

public class DailySalesReportDto
{
    public DateOnly ReportDate { get; set; }
    public int TotalCompletedOrders { get; set; }
    public int TotalSoldQuantity { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<TopProductRow> TopProducts { get; set; } = Array.Empty<TopProductRow>();
}

public class TopProductRow
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int SoldQuantity { get; set; }
}
