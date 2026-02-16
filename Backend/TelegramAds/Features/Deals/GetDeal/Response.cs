namespace TelegramAds.Features.Deals.GetDeal;

public sealed record GetDealResponse(
    Guid Id,
    Guid? ListingId,
    Guid? ChannelId,
    Guid? CampaignId,
    Guid AdvertiserUserId,
    Guid ChannelOwnerUserId,
    string Status,
    decimal AgreedPriceInTon,
    string AdFormat,
    DateTime? ScheduledPostTime,
    string? EscrowAddress,
    DateTime? PostedAt,
    DateTime? VerifiedAt,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string? CreativeText,
    string? CreativeRejectionReason,
    GetDealCampaignDto? Campaign,
    GetDealListingDto? Listing,
    List<DealProposalDto> Proposals,
    List<DealEventDto> Events
);

public sealed record GetDealCampaignDto(
    Guid Id,
    string Title,
    string Brief,
    decimal BudgetInTon,
    string Status,
    DateTime? ScheduleStart,
    DateTime? ScheduleEnd
);

public sealed record GetDealListingDto(
    Guid? Id,
    string? Title,
    string? Description,
    string? Status,
    GetDealChannelDto? Channel
);

public sealed record GetDealChannelDto(
    Guid Id,
    string Username,
    string Title,
    string? Description,
    string Status,
    GetDealChannelStatsDto? Stats
);

public sealed record GetDealChannelStatsDto(
    int SubscriberCount,
    double AvgViewsPerPost,
    double AvgReachPerPost,
    DateTime FetchedAt
);

public sealed record DealProposalDto(
    Guid Id,
    Guid ProposerUserId,
    decimal ProposedPriceInTon,
    DateTime? ProposedSchedule,
    string? Terms,
    string Status,
    DateTime CreatedAt
);

public sealed record DealEventDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string? EventType,
    DateTime CreatedAt
);
