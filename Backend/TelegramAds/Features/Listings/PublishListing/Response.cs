namespace TelegramAds.Features.Listings.PublishListing;

public sealed record PublishListingResponse(
    Guid Id,
    string Title,
    string Status
);
