namespace VTCStockManagementCase.Application.Contracts.Reports;

public class CriticalStockReportDto
{
    public IReadOnlyList<CriticalStockRow> Items { get; set; } = Array.Empty<CriticalStockRow>();
}

public class CriticalStockRow
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public int AvailableStock { get; set; }
    public int Threshold { get; set; }
    public DateTime DetectedAtUtc { get; set; }
}
