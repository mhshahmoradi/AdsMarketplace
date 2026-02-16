namespace TelegramAds.Shared.Db;

public enum DealEventType
{
    Created,
    ProposalSent,
    ProposalAccepted,
    ProposalRejected,
    PaymentRequested,
    PaymentConfirmed,
    PaymentReceived,
    CreativeSubmitted,
    CreativeApproved,
    CreativeRejected,
    CreativeEditsRequested,
    Scheduled,
    Posted,
    Verified,
    Released,
    Refunded,
    Cancelled,
    Expired
}

public sealed class DealEvent
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public DealEventType EventType { get; set; }
    public DealStatus? FromStatus { get; set; }
    public DealStatus? ToStatus { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }

    public Deal Deal { get; set; } = null!;
    public ApplicationUser? ActorUser { get; set; }
}
