namespace VTCStockManagementCase.Application.Abstractions;

public interface ICriticalStockJob
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
