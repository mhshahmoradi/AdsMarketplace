namespace TelegramAds.Features.Bot.Notifications;

public enum BotMessageType
{
    ProposalReceived,
    ProposalAccepted,
    ProposalRejected,
    PaymentReceived,
    CreativeSubmitted,
    CreativeApproved,
    CreativeEditsRequested,
    PostScheduled,
    PostPublished,
    PostVerified,
    FundsReleased,
    FundsRefunded,
    DealCreated,
    DealCancelled,
    DealExpired
}

public sealed class BotMessagePayload
{
    public long ChatId { get; set; }
    public BotMessageType Type { get; set; }
    public Guid? DealId { get; set; }
    public Guid? ProposalId { get; set; }
    public string? ChannelTitle { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public string? CreativePreview { get; set; }
    public string? PostUrl { get; set; }
    public string? Reason { get; set; }
}
