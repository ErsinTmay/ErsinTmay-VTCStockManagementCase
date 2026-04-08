namespace VTCStockManagementCase.Application.Abstractions;

public interface IOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
