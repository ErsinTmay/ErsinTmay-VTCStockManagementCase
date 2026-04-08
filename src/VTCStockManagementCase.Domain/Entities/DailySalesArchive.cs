namespace VTCStockManagementCase.Domain.Entities;

public class DailySalesArchive
{
    public Guid Id { get; set; }
    public DateOnly ReportDate { get; set; }
    public int TotalCompletedOrders { get; set; }
    public int TotalSoldQuantity { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string TopProductsJson { get; set; } = null!;
}
