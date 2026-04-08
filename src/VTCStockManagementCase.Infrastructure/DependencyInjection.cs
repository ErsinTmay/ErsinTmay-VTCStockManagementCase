using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Infrastructure.Options;
using VTCStockManagementCase.Infrastructure.Persistence;
using VTCStockManagementCase.Infrastructure.Services;

namespace VTCStockManagementCase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IShippingService, ShippingQueryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IOutboxMetrics, OutboxMetricsService>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<ICriticalStockJob, CriticalStockJob>();
        services.AddScoped<IDailySalesJob, DailySalesJob>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}
