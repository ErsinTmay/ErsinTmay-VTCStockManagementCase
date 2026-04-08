using Microsoft.Extensions.Options;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Infrastructure.Options;

namespace VTCStockManagementCase.Api.Hosted;

public class CriticalStockHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AppOptions> _options;
    private readonly ILogger<CriticalStockHostedService> _logger;

    public CriticalStockHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppOptions> options,
        ILogger<CriticalStockHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.Value.CriticalStockIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var job = scope.ServiceProvider.GetRequiredService<ICriticalStockJob>();
                await job.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical stock job error");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
