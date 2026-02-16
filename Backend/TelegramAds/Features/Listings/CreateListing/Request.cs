namespace TelegramAds.Features.Listings.CreateListing;

public sealed record CreateListingRequest(
    Guid ChannelId,
    string Title,
    string? Description,
    List<AdFormatRequest> AdFormats
);

public sealed record AdFormatRequest(
    string FormatType,
    decimal PriceInTon,
    int? DurationHours,
    string? Terms
);
