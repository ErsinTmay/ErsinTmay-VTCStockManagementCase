namespace VTCStockManagementCase.Infrastructure.Options;

public class AppOptions
{
    public const string SectionName = "App";

    public int CriticalStockThreshold { get; set; } = 10;

    /// <summary>Outbox polling interval in milliseconds.</summary>
    public int OutboxPollMs { get; set; } = 2000;

    /// <summary>Hourly job interval.</summary>
    public int CriticalStockIntervalMinutes { get; set; } = 60;

    /// <summary>Seeds demo data on app startup (Development only).</summary>
    public bool SeedOnStartup { get; set; } = true;
}
