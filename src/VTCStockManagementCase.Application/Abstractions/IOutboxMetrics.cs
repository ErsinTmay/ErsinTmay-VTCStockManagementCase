namespace VTCStockManagementCase.Application.Abstractions;

public interface IOutboxMetrics
{
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}
