using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TelegramAds.Features.Bot.Notifications;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Outbox;
using TelegramAds.Shared.Time;

namespace TelegramAds.Workers;

public sealed class OutboxDispatcherWorker : IInvocable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherWorker> _logger;

    public OutboxDispatcherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Invoke()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var botDispatcher = scope.ServiceProvider.GetRequiredService<BotMessageDispatcher>();

        var messages = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending ||
                        (m.Status == OutboxMessageStatus.Failed && m.NextRetryAt <= clock.UtcNow))
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync();

        foreach (var message in messages)
        {
            try
            {
                message.Status = OutboxMessageStatus.Processing;
                await db.SaveChangesAsync();

                await DispatchMessageAsync(message, botDispatcher);

                message.Status = OutboxMessageStatus.Completed;
                message.ProcessedAt = clock.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                message.Status = OutboxMessageStatus.Failed;
                message.RetryCount++;
                message.LastError = ex.Message;
                message.NextRetryAt = clock.UtcNow.AddMinutes(Math.Pow(2, message.RetryCount));
            }

            await db.SaveChangesAsync();
        }
    }

    private async Task DispatchMessageAsync(OutboxMessage message, BotMessageDispatcher botDispatcher)
    {
        switch (message.MessageType)
        {
            case "BotNotification":
                await botDispatcher.DispatchAsync(message.PayloadJson);
                break;
            default:
                _logger.LogWarning("Unknown message type: {Type}", message.MessageType);
                break;
        }
    }
}
