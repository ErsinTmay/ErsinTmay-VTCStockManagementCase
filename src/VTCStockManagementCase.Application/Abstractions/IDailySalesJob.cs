namespace VTCStockManagementCase.Application.Abstractions;

public interface IDailySalesJob
{
    /// <summary>Archives sales for the UTC calendar day that ended just before <paramref name="utcNow"/> (yesterday if job runs after midnight).</summary>
    Task RunForPreviousUtcDayAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
