namespace TelegramAds.Features.Listings.ListListingApplications;

public sealed record ListListingApplicationsRequest(
    string? Role,
    string? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20
);
