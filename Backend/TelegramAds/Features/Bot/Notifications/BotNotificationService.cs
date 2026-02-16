using System.Text.Json;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Outbox;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Bot.Notifications;

public sealed class BotNotificationService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public BotNotificationService(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task SendProposalReceivedAsync(long chatId, Guid proposalId, string channelTitle, decimal price, DateTime? schedule)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.ProposalReceived,
            ProposalId = proposalId,
            ChannelTitle = channelTitle,
            Amount = price,
            ScheduledTime = schedule
        });
    }

    public async Task SendProposalAcceptedAsync(long chatId, Guid dealId, string channelTitle, decimal price)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.ProposalAccepted,
            DealId = dealId,
            ChannelTitle = channelTitle,
            Amount = price
        });
    }

    public async Task SendProposalRejectedAsync(long chatId, Guid proposalId, string channelTitle)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.ProposalRejected,
            ProposalId = proposalId,
            ChannelTitle = channelTitle
        });
    }

    public async Task SendPaymentReceivedAsync(long chatId, Guid dealId, decimal amount)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.PaymentReceived,
            DealId = dealId,
            Amount = amount
        });
    }

    public async Task SendCreativeSubmittedAsync(long chatId, Guid dealId, string channelTitle, string creativePreview)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.CreativeSubmitted,
            DealId = dealId,
            ChannelTitle = channelTitle,
            CreativePreview = creativePreview
        });
    }

    public async Task SendCreativeApprovedAsync(long chatId, Guid dealId, string channelTitle)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.CreativeApproved,
            DealId = dealId,
            ChannelTitle = channelTitle
        });
    }

    public async Task SendCreativeEditsRequestedAsync(long chatId, Guid dealId, string channelTitle, string reason)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.CreativeEditsRequested,
            DealId = dealId,
            ChannelTitle = channelTitle,
            Reason = reason
        });
    }

    public async Task SendPostScheduledAsync(long chatId, Guid dealId, string channelTitle, DateTime scheduledTime)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.PostScheduled,
            DealId = dealId,
            ChannelTitle = channelTitle,
            ScheduledTime = scheduledTime
        });
    }

    public async Task SendPostPublishedAsync(long chatId, Guid dealId, string channelTitle, string postUrl)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.PostPublished,
            DealId = dealId,
            ChannelTitle = channelTitle,
            PostUrl = postUrl
        });
    }

    public async Task SendPostVerifiedAsync(long chatId, Guid dealId, string channelTitle)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.PostVerified,
            DealId = dealId,
            ChannelTitle = channelTitle
        });
    }

    public async Task SendFundsReleasedAsync(long chatId, Guid dealId, string channelTitle, decimal amount)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.FundsReleased,
            DealId = dealId,
            ChannelTitle = channelTitle,
            Amount = amount
        });
    }

    public async Task SendFundsRefundedAsync(long chatId, Guid dealId, string channelTitle, decimal amount, string reason)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.FundsRefunded,
            DealId = dealId,
            ChannelTitle = channelTitle,
            Amount = amount,
            Reason = reason
        });
    }

    public async Task SendDealCreatedAsync(long chatId, Guid dealId, string channelTitle, decimal price)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.DealCreated,
            DealId = dealId,
            ChannelTitle = channelTitle,
            Amount = price
        });
    }

    public async Task SendDealCancelledAsync(long chatId, Guid dealId, string channelTitle, string reason)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.DealCancelled,
            DealId = dealId,
            ChannelTitle = channelTitle,
            Reason = reason
        });
    }

    public async Task SendDealExpiredAsync(long chatId, Guid dealId, string channelTitle)
    {
        await QueueMessageAsync(new BotMessagePayload
        {
            ChatId = chatId,
            Type = BotMessageType.DealExpired,
            DealId = dealId,
            ChannelTitle = channelTitle
        });
    }

    private async Task QueueMessageAsync(BotMessagePayload payload)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "BotNotification",
            PayloadJson = JsonSerializer.Serialize(payload),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        _db.OutboxMessages.Add(message);
        await _db.SaveChangesAsync();
    }
}
