namespace TelegramAds.Features.Listings.ListListingApplications;

public sealed record ListListingApplicationsResponse(
    List<ListingApplicationSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record ListingApplicationSummaryDto(
    Guid Id,
    string Status,
    decimal ProposedPriceInTon,
    string? Message,
    DateTime CreatedAt,
    ApplicationListingDto Listing,
    ListingAppCampaignDto Campaign
);

public sealed record ApplicationListingDto(
    Guid Id,
    string Title,
    string? Description,
    string ChannelUsername,
    string ChannelTitle
);

public sealed record ListingAppCampaignDto(
    Guid Id,
    string Title,
    string Brief,
    decimal BudgetInTon
);
