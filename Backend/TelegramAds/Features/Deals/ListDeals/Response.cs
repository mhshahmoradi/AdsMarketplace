namespace TelegramAds.Features.Deals.ListDeals;

public sealed record ListDealsResponse(
    List<DealSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record DealSummaryDto(
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
    DateTime UpdatedAt,
    DateTime? ExpiresAt,
    ListDealCampaignDto? Campaign,
    ListDealListingDto? Listing
);

public sealed record ListDealCampaignDto(
    Guid? Id,
    string? Title,
    string? Brief,
    decimal BudgetInTon,
    string? Status,
    DateTime? ScheduleStart,
    DateTime? ScheduleEnd
);

public sealed record ListDealListingDto(
    Guid? Id,
    string? Title,
    string? Description,
    string? Status,
    ListDealChannelDto? Channel
);

public sealed record ListDealChannelDto(
    Guid Id,
    string Username,
    string Title,
    string? Description,
    string Status,
    ListDealChannelStatsDto? Stats
);

public sealed record ListDealChannelStatsDto(
    int SubscriberCount,
    double AvgViewsPerPost,
    double AvgReachPerPost,
    DateTime FetchedAt
);
