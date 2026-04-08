using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public OutboxMessageStatus Status { get; set; }
}
