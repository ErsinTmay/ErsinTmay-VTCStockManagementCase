namespace VTCStockManagementCase.Domain.Entities;

public class CriticalStockRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public int AvailableStock { get; set; }
    public int Threshold { get; set; }
    public DateTime DetectedAtUtc { get; set; }
}
