namespace TelegramAds.Features.Listings.ApplyToListing;

public sealed record ApplyToListingResponse(
    Guid ApplicationId,
    Guid ListingId,
    Guid CampaignId,
    string Status,
    decimal ProposedPriceInTon,
    DateTime CreatedAt
);
