namespace TelegramAds.Features.Listings.UpdateListing;

public sealed record UpdateListingResponse(
    Guid Id,
    Guid ChannelId,
    string Title,
    string? Description,
    string Status,
    List<AdFormatDto> AdFormats,
    DateTime UpdatedAt
);

public sealed record AdFormatDto(
    Guid Id,
    string FormatType,
    decimal PriceInTon,
    int? DurationHours,
    string? Terms
);
