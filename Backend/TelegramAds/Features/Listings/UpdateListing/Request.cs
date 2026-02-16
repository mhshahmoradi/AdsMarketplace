using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.UpdateListing;

public sealed record UpdateListingRequest(
    string? Title,
    string? Description,
    string? Status,
    List<AdFormatUpdateRequest>? AdFormats
);

public sealed record AdFormatUpdateRequest(
    string FormatType,
    decimal PriceInTon,
    int? DurationHours,
    string? Terms
);
