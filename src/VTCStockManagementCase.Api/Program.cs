using VTCStockManagementCase.Api.Hosted;
using VTCStockManagementCase.Api.Middleware;
using VTCStockManagementCase.Infrastructure;
using VTCStockManagementCase.Infrastructure.Options;
using VTCStockManagementCase.Infrastructure.Persistence;
using VTCStockManagementCase.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "VTC Stock Management API", Version = "v1" });
});

builder.Services.AddHealthChecks();

builder.Services.AddInfrastructure(builder.Configuration);

//DI bağımlılıkları
builder.Services.AddHostedService<OutboxProcessorHostedService>();
builder.Services.AddHostedService<CriticalStockHostedService>();
builder.Services.AddHostedService<DailySalesHostedService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppOptions>>().Value;
        if (options.SeedOnStartup)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
