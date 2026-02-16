namespace TelegramAds.Features.Listings.CreateListing;

public sealed record CreateListingResponse(
    Guid Id,
    Guid ChannelId,
    string Title,
    string Status
);
