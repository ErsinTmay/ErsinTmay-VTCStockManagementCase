using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTCStockManagementCase.Application;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Domain.Entities;
using VTCStockManagementCase.Domain.Enums;
using VTCStockManagementCase.Infrastructure.Persistence;

namespace VTCStockManagementCase.Infrastructure.Services;

public class OutboxProcessor : IOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly AppDbContext _db;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(AppDbContext db, ILogger<OutboxProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _db.OutboxMessages.AsNoTracking()
            .Where(x => x.Status == OutboxMessageStatus.Pending)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(25)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                var locked = await _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (locked == null || locked.Status != OutboxMessageStatus.Pending)
                {
                    await tx.CommitAsync(cancellationToken);
                    continue;
                }
                //Eğer sipariş tamamlandıysa, kargo hazırlığı oluşturulur.
                //Bekleme yapmadan diğer işlemler yapılır.
                if (locked.Type == OutboxTypes.OrderCompleted)
                {
                    var payload = JsonSerializer.Deserialize<OrderCompletedPayload>(locked.Payload, JsonOpts);
                    if (payload?.OrderId is Guid orderId)
                        await AddShippingPreparationIfMissingAsync(orderId, cancellationToken);
                }

                locked.Status = OutboxMessageStatus.Processed;
                locked.ProcessedAtUtc = DateTime.UtcNow;
                locked.LastError = null;

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                _logger.LogInformation("Outbox message processed {MessageId} {Type}", locked.Id, locked.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing failed {MessageId}", id);
                try
                {
                    await using var tx2 = await _db.Database.BeginTransactionAsync(cancellationToken);
                    var m = await _db.OutboxMessages.FirstAsync(x => x.Id == id, cancellationToken);
                    m.RetryCount++;
                    m.LastError = ex.Message;
                    if (m.RetryCount >= 10)
                        m.Status = OutboxMessageStatus.Failed;
                    await _db.SaveChangesAsync(cancellationToken);
                    await tx2.CommitAsync(cancellationToken);
                }
                catch (Exception inner)
                {
                    _logger.LogError(inner, "Failed to persist outbox retry for {MessageId}", id);
                }
            }
        }
    }

    private async Task AddShippingPreparationIfMissingAsync(Guid orderId, CancellationToken cancellationToken)
    {
        // Eğer varsa birden fazla kargo hazırlığı oluşturulmaz. duplicate durumunda hata verilir.
        if (await _db.ShippingPreparations.AnyAsync(x => x.OrderId == orderId, cancellationToken))
        {
            _logger.LogInformation("Shipping preparation already exists for order {OrderId}", orderId);
            return;
        }

        var now = DateTime.UtcNow;
        _db.ShippingPreparations.Add(new ShippingPreparation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = ShippingPreparationStatus.Prepared,
            CreatedAtUtc = now,
            ProcessedAtUtc = now,
            ErrorMessage = null
        });
    }

    private sealed record OrderCompletedPayload(Guid OrderId);
}
