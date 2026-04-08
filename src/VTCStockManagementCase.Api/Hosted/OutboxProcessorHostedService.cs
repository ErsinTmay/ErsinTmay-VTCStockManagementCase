using Microsoft.Extensions.Options;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Infrastructure.Options;

namespace VTCStockManagementCase.Api.Hosted;

public class OutboxProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AppOptions> _options;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    public OutboxProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppOptions> options,
        ILogger<OutboxProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(Math.Max(200, _options.Value.OutboxPollMs));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor loop error");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
