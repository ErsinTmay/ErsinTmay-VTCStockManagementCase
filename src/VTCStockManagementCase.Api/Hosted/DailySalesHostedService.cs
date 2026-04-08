using Microsoft.EntityFrameworkCore;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Api.Hosted;

/// <summary>
/// Ensures daily sales archives are generated for any missing UTC days.
/// </summary>
public class DailySalesHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySalesHostedService> _logger;

    public DailySalesHostedService(IServiceScopeFactory scopeFactory, ILogger<DailySalesHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CatchUpMissingDaysAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily sales job loop error");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CatchUpMissingDaysAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var latestTargetDate = DateOnly.FromDateTime(utcNow.AddDays(-1));
        if (latestTargetDate == DateOnly.MinValue)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IDailySalesJob>();

        var lastArchived = await db.DailySalesArchives
            .AsNoTracking()
            .OrderByDescending(x => x.ReportDate)
            .Select(x => (DateOnly?)x.ReportDate)
            .FirstOrDefaultAsync(cancellationToken);

        // No archive yet: initialize with latest day only to avoid generating unbounded history.
        var nextDate = lastArchived?.AddDays(1) ?? latestTargetDate;
        if (nextDate > latestTargetDate)
            return;

        for (var date = nextDate; date <= latestTargetDate; date = date.AddDays(1))
        {
            var syntheticUtcNow = date.ToDateTime(new TimeOnly(0, 1), DateTimeKind.Utc).AddDays(1);
            await job.RunForPreviousUtcDayAsync(syntheticUtcNow, cancellationToken);
            _logger.LogInformation("Daily sales archive generated for missing day {ReportDate}", date);
        }
    }
}
