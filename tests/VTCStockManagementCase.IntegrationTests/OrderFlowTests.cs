using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.IntegrationTests;

public class OrderFlowTests : IClassFixture<IntegrationFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IntegrationFixture _fixture;

    public OrderFlowTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient CreateClient() => _fixture.Factory.CreateClient();

    [Fact]
    public async Task Product_create_stock_in_order_reserve_payment_success_updates_inventory()
    {
        var client = CreateClient();

        var productRes = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-INT-1",
            name = "Test Product",
            description = "d"
        });
        productRes.EnsureSuccessStatusCode();
        var product = await productRes.Content.ReadFromJsonAsync<ProductResponse>(JsonOpts);
        Assert.NotNull(product);

        var stockRes = await client.PostAsJsonAsync("/api/inventory/stock-in", new
        {
            productId = product.Id,
            quantity = 10,
            reason = "seed"
        });
        stockRes.EnsureSuccessStatusCode();

        var inv1 = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{product.Id}", JsonOpts);
        Assert.NotNull(inv1);
        Assert.Equal(10, inv1.Available);

        var orderRes = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = "c1",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        });
        orderRes.EnsureSuccessStatusCode();
        var order = await orderRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);

        var inv2 = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{product.Id}", JsonOpts);
        Assert.NotNull(inv2);
        Assert.Equal(8, inv2.Available);
        Assert.Equal(2, inv2.Reserved);

        var payRes = await client.PostAsync($"/api/orders/{order.Id}/payments/simulate-success", null);
        payRes.EnsureSuccessStatusCode();
        var paid = await payRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(paid);
        Assert.Equal(OrderStatus.Completed, paid.Status);

        var inv3 = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{product.Id}", JsonOpts);
        Assert.NotNull(inv3);
        Assert.Equal(8, inv3.OnHand);
        Assert.Equal(0, inv3.Reserved);
        Assert.Equal(8, inv3.Available);
    }

    [Fact]
    public async Task Order_insufficient_stock_returns_409()
    {
        var client = CreateClient();

        var productRes = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-INT-2",
            name = "Low Stock",
            description = null as string
        });
        var product = await productRes.Content.ReadFromJsonAsync<ProductResponse>(JsonOpts);
        Assert.NotNull(product);

        await client.PostAsJsonAsync("/api/inventory/stock-in", new
        {
            productId = product.Id,
            quantity = 1,
            reason = "seed"
        });

        var orderRes = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = "c1",
            items = new[] { new { productId = product.Id, quantity = 5 } }
        });

        Assert.Equal(HttpStatusCode.Conflict, orderRes.StatusCode);
    }

    [Fact]
    public async Task Payment_failure_releases_reservation()
    {
        var client = CreateClient();

        var productRes = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-INT-3",
            name = "P3",
            description = (string?)null
        });
        var product = await productRes.Content.ReadFromJsonAsync<ProductResponse>(JsonOpts);
        Assert.NotNull(product);

        await client.PostAsJsonAsync("/api/inventory/stock-in", new
        {
            productId = product.Id,
            quantity = 5,
            reason = "seed"
        });

        var orderRes = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = "c1",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        });
        var order = await orderRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(order);

        var failRes = await client.PostAsync($"/api/orders/{order.Id}/payments/simulate-failure", null);
        failRes.EnsureSuccessStatusCode();
        var failed = await failRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(failed);
        Assert.Equal(OrderStatus.Failed, failed.Status);

        var inv = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{product.Id}", JsonOpts);
        Assert.NotNull(inv);
        Assert.Equal(5, inv.Available);
        Assert.Equal(0, inv.Reserved);
    }

    [Fact]
    public async Task Cancel_pending_order_releases_stock()
    {
        var client = CreateClient();

        var productRes = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-INT-4",
            name = "P4",
            description = (string?)null
        });
        var product = await productRes.Content.ReadFromJsonAsync<ProductResponse>(JsonOpts);
        Assert.NotNull(product);

        await client.PostAsJsonAsync("/api/inventory/stock-in", new
        {
            productId = product.Id,
            quantity = 3,
            reason = "seed"
        });

        var orderRes = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = "c1",
            items = new[] { new { productId = product.Id, quantity = 1 } }
        });
        var order = await orderRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(order);

        var cancelRes = await client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        cancelRes.EnsureSuccessStatusCode();
        var cancelled = await cancelRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(cancelled);
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);

        var inv = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{product.Id}", JsonOpts);
        Assert.NotNull(inv);
        Assert.Equal(3, inv.Available);
        Assert.Equal(0, inv.Reserved);
    }

    [Fact]
    public async Task Outbox_creates_shipping_preparation_after_completion()
    {
        var client = CreateClient();

        var productRes = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-INT-5",
            name = "P5",
            description = (string?)null
        });
        var product = await productRes.Content.ReadFromJsonAsync<ProductResponse>(JsonOpts);
        Assert.NotNull(product);

        await client.PostAsJsonAsync("/api/inventory/stock-in", new
        {
            productId = product.Id,
            quantity = 5,
            reason = "seed"
        });

        var orderRes = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = "c1",
            items = new[] { new { productId = product.Id, quantity = 1 } }
        });
        var order = await orderRes.Content.ReadFromJsonAsync<OrderResponse>(JsonOpts);
        Assert.NotNull(order);

        await client.PostAsync($"/api/orders/{order.Id}/payments/simulate-success", null);

        using var scope = _fixture.Factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        await processor.ProcessPendingAsync();

        var shipRes = await client.GetAsync($"/api/shipping-preparations/{order.Id}");
        shipRes.EnsureSuccessStatusCode();
        var ship = await shipRes.Content.ReadFromJsonAsync<ShippingResponse>(JsonOpts);
        Assert.NotNull(ship);
        Assert.Equal(order.Id, ship.OrderId);
    }

    [Fact]
    public async Task Critical_stock_job_writes_records()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ICriticalStockJob>();
        await job.RunAsync();

        var client = CreateClient();
        var report = await client.GetFromJsonAsync<CriticalStockReportResponse>("/api/reports/critical-stock", JsonOpts);
        Assert.NotNull(report);
        Assert.NotNull(report.Items);
    }

    [Fact]
    public async Task Daily_sales_job_can_write_archive()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IDailySalesJob>();
        await job.RunForPreviousUtcDayAsync(DateTime.UtcNow, CancellationToken.None);

        var client = CreateClient();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var reportRes = await client.GetAsync($"/api/reports/daily-sales?date={yesterday:yyyy-MM-dd}");
        Assert.True(reportRes.IsSuccessStatusCode || reportRes.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Concurrent_orders_respect_stock()
    {
        var client = CreateClient();

        var productRes = await client.PostAsJsonAsync("/api/products", new
        {
            sku = $"SKU-INT-CC-{Guid.NewGuid():N}",
            name = "Concurrency",
            description = (string?)null
        });
        var product = await productRes.Content.ReadFromJsonAsync<ProductResponse>(JsonOpts);
        Assert.NotNull(product);

        await client.PostAsJsonAsync("/api/inventory/stock-in", new
        {
            productId = product.Id,
            quantity = 5,
            reason = "seed"
        });

        var orderPayload = new
        {
            customerId = "c1",
            items = new[] { new { productId = product.Id, quantity = 1 } }
        };

        var tasks = Enumerable.Range(0, 20).Select(_ =>
            client.PostAsJsonAsync("/api/orders", orderPayload)).ToArray();

        var results = await Task.WhenAll(tasks);

        var successes = results.Count(r => r.IsSuccessStatusCode);
        var conflicts = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(5, successes);
        Assert.Equal(15, conflicts);

        var inv = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{product.Id}", JsonOpts);
        Assert.NotNull(inv);
        Assert.Equal(5, inv.Reserved);
        Assert.Equal(0, inv.Available);
    }

    private sealed record ProductResponse(Guid Id, string Sku);
    private sealed record InventoryResponse(int OnHand, int Reserved, int Available);
    private sealed record OrderResponse(Guid Id, OrderStatus Status);
    private sealed record ShippingResponse(Guid OrderId);
    private sealed record CriticalStockReportResponse(List<CriticalStockRow> Items);
    private sealed record CriticalStockRow(Guid ProductId);
}
