using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Domain.Entities;

public class ShippingPreparation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ShippingPreparationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
