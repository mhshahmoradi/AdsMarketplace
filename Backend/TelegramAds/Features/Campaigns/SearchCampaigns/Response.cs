namespace TelegramAds.Features.Campaigns.SearchCampaigns;

public sealed record SearchCampaignsResponse(
    List<SearchCampaignItem> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record SearchCampaignItem(
    Guid Id,
    string Title,
    string Brief,
    decimal BudgetInTon,
    int? MinSubscribers,
    int? MinAvgViews,
    string? TargetLanguages,
    DateTime? ScheduleStart,
    DateTime? ScheduleEnd,
    string Status,
    int ApplicationCount,
    bool IsOwner,
    DateTime CreatedAt
);
