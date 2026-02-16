using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Workers;

public sealed class DealExpiryWorker : IInvocable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DealExpiryWorker> _logger;

    public DealExpiryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DealExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Invoke()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var staleDeals = await db.Deals
            .Where(d => d.Status == DealStatus.Agreed || d.Status == DealStatus.AwaitingPayment)
            .Where(d => d.ExpiresAt != null && d.ExpiresAt <= clock.UtcNow)
            .ToListAsync();

        foreach (var deal in staleDeals)
        {
            var previousStatus = deal.Status;
            deal.Status = DealStatus.Expired;
            deal.UpdatedAt = clock.UtcNow;

            db.DealEvents.Add(new DealEvent
            {
                Id = Guid.NewGuid(),
                DealId = deal.Id,
                FromStatus = previousStatus,
                ToStatus = DealStatus.Expired,
                EventType = DealEventType.Expired,
                CreatedAt = clock.UtcNow
            });

            _logger.LogInformation("Deal {DealId} expired from status {Status}", deal.Id, previousStatus);
        }

        if (staleDeals.Count > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Expired {Count} stale deals", staleDeals.Count);
        }
    }
}
