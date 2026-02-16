using System.Text.Json;
using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramAds.Features.Bot.Chat;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Workers;

public sealed class PostSchedulerWorker : IInvocable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<PostSchedulerWorker> _logger;

    public PostSchedulerWorker(
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient botClient,
        ILogger<PostSchedulerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _botClient = botClient;
        _logger = logger;
    }

    public async Task Invoke()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var dueDeals = await db.Deals
            .Include(d => d.Channel)
            .Where(d => d.Status == DealStatus.Scheduled)
            .Where(d => d.ScheduledPostTime != null && d.ScheduledPostTime <= clock.UtcNow)
            .ToListAsync();

        _logger.LogInformation("Found {Count} deals due for posting", dueDeals.Count);

        foreach (var deal in dueDeals)
        {
            try
            {
                if (string.IsNullOrEmpty(deal.CreativeText) && string.IsNullOrEmpty(deal.CreativeMediaJson))
                {
                    _logger.LogWarning("Deal {DealId} has no creative content, skipping", deal.Id);
                    continue;
                }

                _logger.LogInformation("Posting deal {DealId} to channel {ChannelId}", deal.Id, deal.Channel!.TgChannelId);

                int postedMessageId;

                if (!string.IsNullOrEmpty(deal.CreativeMediaJson))
                {
                    var mediaItems = JsonSerializer.Deserialize<List<CreativeMediaItem>>(deal.CreativeMediaJson, JsonOptions);
                    postedMessageId = await PostMediaAsync(deal, mediaItems);
                }
                else
                {
                    var message = await _botClient.SendMessage(
                        deal.Channel!.TgChannelId,
                        deal.CreativeText!,
                        parseMode: ParseMode.Html);
                    postedMessageId = message.MessageId;
                }

                var previousStatus = deal.Status;
                deal.Status = DealStatus.Posted;
                deal.PostedMessageId = postedMessageId;
                deal.PostedAt = clock.UtcNow;
                deal.UpdatedAt = clock.UtcNow;

                db.DealEvents.Add(new DealEvent
                {
                    Id = Guid.NewGuid(),
                    DealId = deal.Id,
                    FromStatus = previousStatus,
                    ToStatus = DealStatus.Posted,
                    EventType = DealEventType.Posted,
                    CreatedAt = clock.UtcNow
                });

                _logger.LogInformation("Posted deal {DealId} to channel {ChannelId}, message {MessageId}",
                    deal.Id, deal.Channel.TgChannelId, postedMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post deal {DealId} to channel", deal.Id);
            }
        }

        if (dueDeals.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private async Task<int> PostMediaAsync(Deal deal, List<CreativeMediaItem>? mediaItems)
    {
        if (mediaItems is null or { Count: 0 })
        {
            var msg = await _botClient.SendMessage(
                deal.Channel!.TgChannelId,
                deal.CreativeText ?? string.Empty,
                parseMode: ParseMode.Html);
            return msg.MessageId;
        }

        var item = mediaItems[0];
        var sentMsg = item.Type switch
        {
            "photo" => await _botClient.SendPhoto(
                deal.Channel!.TgChannelId,
                InputFile.FromFileId(item.FileId),
                caption: deal.CreativeText,
                parseMode: ParseMode.Html),
            "video" => await _botClient.SendVideo(
                deal.Channel!.TgChannelId,
                InputFile.FromFileId(item.FileId),
                caption: deal.CreativeText,
                parseMode: ParseMode.Html),
            "document" => await _botClient.SendDocument(
                deal.Channel!.TgChannelId,
                InputFile.FromFileId(item.FileId),
                caption: deal.CreativeText,
                parseMode: ParseMode.Html),
            _ => await _botClient.SendPhoto(
                deal.Channel!.TgChannelId,
                InputFile.FromFileId(item.FileId),
                caption: deal.CreativeText,
                parseMode: ParseMode.Html)
        };
        return sentMsg.MessageId;
    }
}
