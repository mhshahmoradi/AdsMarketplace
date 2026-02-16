namespace TelegramAds.Shared.Db;

public enum DealStatus
{
    Agreed,
    AwaitingPayment,
    Paid,
    CreativeDraft,
    CreativeReview,
    Scheduled,
    Posted,
    Verified,
    Released,
    Refunded,
    Cancelled,
    Expired
}

public sealed class Deal
{
    public Guid Id { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid AdvertiserUserId { get; set; }
    public Guid ChannelOwnerUserId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? CampaignApplicationId { get; set; }
    public Guid? ListingApplicationId { get; set; }
    public DealStatus Status { get; set; }
    public decimal AgreedPriceInTon { get; set; }
    public AdFormatType AdFormat { get; set; }
    public DateTime? ScheduledPostTime { get; set; }
    public string? EscrowAddress { get; set; }
    public string? CreativeText { get; set; }
    public string? CreativeMediaJson { get; set; }
    public string? CreativeRejectionReason { get; set; }
    public long? PostedMessageId { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTime? EscrowReleasedAt { get; set; }
    public DateTime? EscrowRefundedAt { get; set; }

    public Listing? Listing { get; set; } = null!;
    public Channel? Channel { get; set; } = null!;
    public ApplicationUser AdvertiserUser { get; set; } = null!;
    public ApplicationUser ChannelOwnerUser { get; set; } = null!;
    public Campaign? Campaign { get; set; } = null!;
    public CampaignApplication? CampaignApplication { get; set; }
    public ListingApplication? ListingApplication { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<DealProposal> Proposals { get; set; } = [];
    public ICollection<DealEvent> Events { get; set; } = [];
    public ICollection<DealEscrowTransaction> EscrowTransactions { get; set; } = [];
}
