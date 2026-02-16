using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramAds.Features.Bot.Notifications;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Workers;

public sealed class PostVerifierWorker : IInvocable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostVerifierWorker> _logger;
    private readonly IConfiguration _configuration;

    public PostVerifierWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PostVerifierWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Invoke()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var notificationService = scope.ServiceProvider.GetRequiredService<BotNotificationService>();

        var postedDealsAtEndTime = await db.Deals
            .Include(d => d.Channel)
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Campaign)
            .Where(d => d.Status == DealStatus.Posted)
            .Where(d => d.Campaign.ScheduleEnd != null && d.Campaign.ScheduleEnd <= clock.UtcNow)
            .Where(d => d.PostedMessageId != null)
            .ToListAsync();

        foreach (var deal in postedDealsAtEndTime)
        {
            try
            {
                await botClient.DeleteMessage(
                    chatId: deal.Channel.TgChannelId,
                    messageId: (int)deal.PostedMessageId!.Value);

                var previousStatus = deal.Status;
                deal.Status = DealStatus.Verified;
                deal.VerifiedAt = clock.UtcNow;
                deal.UpdatedAt = clock.UtcNow;

                var channelOwnerWallet = await db.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == deal.ChannelOwnerUserId);

                if (channelOwnerWallet != null)
                {
                    channelOwnerWallet.Balance += deal.AgreedPriceInTon;
                    channelOwnerWallet.UpdatedAt = clock.UtcNow;
                    _logger.LogInformation("Credited {Amount} TON to channel owner {UserId} wallet for deal {DealId}",
                        deal.AgreedPriceInTon, deal.ChannelOwnerUserId, deal.Id);
                }
                else
                {
                    _logger.LogWarning("Wallet not found for channel owner {UserId} in deal {DealId}",
                        deal.ChannelOwnerUserId, deal.Id);
                }

                db.DealEvents.Add(new DealEvent
                {
                    Id = Guid.NewGuid(),
                    DealId = deal.Id,
                    EventType = DealEventType.Verified,
                    FromStatus = previousStatus,
                    ToStatus = DealStatus.Verified,
                    PayloadJson = $"{{\"deletedAt\":\"{clock.UtcNow:O}\",\"reason\":\"Campaign ended\"}}",
                    CreatedAt = clock.UtcNow
                });

                deal.Status = DealStatus.Released;
                deal.EscrowReleasedAt = clock.UtcNow;

                db.DealEvents.Add(new DealEvent
                {
                    Id = Guid.NewGuid(),
                    DealId = deal.Id,
                    EventType = DealEventType.Released,
                    FromStatus = DealStatus.Verified,
                    ToStatus = DealStatus.Released,
                    CreatedAt = clock.UtcNow
                });

                _logger.LogInformation("Verified and released deal {DealId} at campaign end time", deal.Id);

                await notificationService.SendPostVerifiedAsync(
                    deal.AdvertiserUser.TgUserId,
                    deal.Id,
                    deal.Channel.Title);

                await notificationService.SendFundsReleasedAsync(
                    deal.ChannelOwnerUser.TgUserId,
                    deal.Id,
                    deal.Channel.Title,
                    deal.AgreedPriceInTon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify and delete post for deal {DealId}", deal.Id);
            }
        }

        if (postedDealsAtEndTime.Count > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Verified and released {Count} deals at campaign end time", postedDealsAtEndTime.Count);
        }
    }
}
