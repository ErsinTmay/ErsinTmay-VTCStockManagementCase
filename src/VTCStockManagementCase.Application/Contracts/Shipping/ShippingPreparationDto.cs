using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Application.Contracts.Shipping;

public class ShippingPreparationDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ShippingPreparationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
