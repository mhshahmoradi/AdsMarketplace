namespace TelegramAds.Features.Campaigns.ListCampaignApplications;

public sealed record ListCampaignApplicationsRequest(
    string? Role,
    string? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20
);
