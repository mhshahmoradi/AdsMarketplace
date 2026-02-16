namespace TelegramAds.Features.Listings.GetListingById;

public sealed record GetListingByIdResponse(
    Guid Id,
    Guid ChannelId,
    string ChannelUsername,
    string ChannelTitle,
    string Title,
    string? Description,
    string Status,
    List<ListingAdFormatDto> AdFormats,
    int SubscriberCount,
    double AvgViews,
    double AvgReach,
    string? Language,
    string? LanguagesGraphJson,
    double? PremiumSubscriberCount,
    bool IsMine,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record ListingAdFormatDto(
    Guid Id,
    string FormatType,
    decimal PriceInTon,
    int? DurationHours,
    string? Terms
);
