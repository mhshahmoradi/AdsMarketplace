namespace TelegramAds.Features.Listings.ChangeListingStatus;

public sealed record ChangeListingStatusResponse(
    Guid Id,
    string Status,
    DateTime UpdatedAt
);
