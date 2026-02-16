namespace TelegramAds.Features.Listings.SearchListings;

public sealed record SearchListingsResponse(
    List<ListingDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record ListingDto(
    Guid Id,
    Guid ChannelId,
    string ChannelUsername,
    string ChannelTitle,
    string Title,
    string? Description,
    string Status,
    List<SearchListingAdFormatDto> AdFormats,
    int SubscriberCount,
    double AvgViews,
    double AvgReach,
    string? Language,
    double? PremiumSubscriberCount,
    bool IsOwner
);

public sealed record SearchListingAdFormatDto(
    Guid Id,
    string FormatType,
    decimal PriceInTon,
    int? DurationHours,
    string? Terms
);
