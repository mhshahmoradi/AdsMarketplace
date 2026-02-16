namespace TelegramAds.Features.Listings.ApplyToListing;

public sealed record ApplyToListingRequest(
    Guid ListingId,
    Guid CampaignId,
    decimal ProposedPriceInTon,
    string? Message
);
