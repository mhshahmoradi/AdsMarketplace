using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramAds.Features.Bot.Templates;

namespace TelegramAds.Features.Bot.Notifications;

public sealed class BotMessageDispatcher
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotMessageDispatcher> _logger;

    public BotMessageDispatcher(ITelegramBotClient botClient, ILogger<BotMessageDispatcher> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task DispatchAsync(string payloadJson, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Deserialize<BotMessagePayload>(payloadJson);
        if (payload == null)
        {
            _logger.LogWarning("Failed to deserialize bot message payload");
            return;
        }

        var (text, keyboard) = BuildMessage(payload);

        await _botClient.SendMessage(
            payload.ChatId,
            text,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: ct);

        _logger.LogInformation("Sent {Type} notification to chat {ChatId}", payload.Type, payload.ChatId);
    }

    private static (string Text, ReplyMarkup? Keyboard) BuildMessage(BotMessagePayload payload)
    {
        return payload.Type switch
        {
            BotMessageType.ProposalReceived => (
                MessageTemplates.NewProposalNotification(payload.ChannelTitle!, payload.Amount!.Value, payload.ScheduledTime),
                MessageTemplates.ProposalActionKeyboard(payload.ProposalId!.Value)),

            BotMessageType.ProposalAccepted => (
                MessageTemplates.ProposalAcceptedNotification(payload.ChannelTitle!, payload.Amount!.Value),
                null),

            BotMessageType.ProposalRejected => (
                MessageTemplates.ProposalRejectedNotification(payload.ChannelTitle!),
                null),

            BotMessageType.PaymentReceived => (
                MessageTemplates.PaymentReceivedNotification(payload.DealId!.Value, payload.Amount!.Value),
                null),

            BotMessageType.CreativeSubmitted => (
                MessageTemplates.CreativeSubmittedNotification(payload.ChannelTitle!, payload.CreativePreview!),
                MessageTemplates.CreativeReviewKeyboard(payload.DealId!.Value)),

            BotMessageType.CreativeApproved => (
                MessageTemplates.CreativeApprovedNotification(payload.ChannelTitle!),
                null),

            BotMessageType.CreativeEditsRequested => (
                MessageTemplates.CreativeEditsRequestedNotification(payload.ChannelTitle!, payload.Reason!),
                null),

            BotMessageType.PostScheduled => (
                MessageTemplates.PostScheduledNotification(payload.ChannelTitle!, payload.ScheduledTime!.Value),
                null),

            BotMessageType.PostPublished => (
                MessageTemplates.PostPublishedNotification(payload.ChannelTitle!, payload.PostUrl!),
                null),

            BotMessageType.PostVerified => (
                MessageTemplates.PostVerifiedNotification(payload.ChannelTitle!),
                null),

            BotMessageType.FundsReleased => (
                MessageTemplates.FundsReleasedNotification(payload.Amount!.Value, payload.ChannelTitle!),
                null),

            BotMessageType.FundsRefunded => (
                MessageTemplates.FundsRefundedNotification(payload.Amount!.Value, payload.ChannelTitle!, payload.Reason!),
                null),

            BotMessageType.DealCreated => (
                MessageTemplates.DealCreatedNotification(payload.ChannelTitle!, payload.Amount!.Value, payload.DealId!.Value),
                null),

            BotMessageType.DealCancelled => (
                MessageTemplates.DealCancelledNotification(payload.ChannelTitle!, payload.Reason!),
                null),

            BotMessageType.DealExpired => (
                MessageTemplates.DealExpiredNotification(payload.ChannelTitle!),
                null),

            _ => ($"Notification: {payload.Type}", null)
        };
    }
}
