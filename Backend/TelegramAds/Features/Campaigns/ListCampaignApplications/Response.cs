namespace TelegramAds.Features.Campaigns.ListCampaignApplications;

public sealed record ListCampaignApplicationsResponse(
    List<CampaignApplicationSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record CampaignApplicationSummaryDto(
    Guid Id,
    string Status,
    decimal ProposedPriceInTon,
    string? Message,
    DateTime CreatedAt,
    ApplicationChannelDto Channel,
    CampaignAppCampaignDto Campaign
);

public sealed record ApplicationChannelDto(
    Guid Id,
    string Username,
    string Title,
    int SubscriberCount,
    double AvgViews
);

public sealed record CampaignAppCampaignDto(
    Guid Id,
    string Title,
    string Brief,
    decimal BudgetInTon
);
