using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.IntegrationTests;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

        await _container.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                    ["App:OutboxPollMs"] = "30000",
                    ["App:CriticalStockIntervalMinutes"] = "1440",
                    ["App:SeedOnStartup"] = "false"
                });
            });

        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
            await Factory.DisposeAsync();
        if (_container != null)
            await _container.DisposeAsync();
    }
}
