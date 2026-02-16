namespace TelegramAds.Features.Campaigns.GetCampaigns;

public sealed record GetCampaignsResponse(List<CampaignItem> Items, int TotalCount);

public sealed record CampaignItem(
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
