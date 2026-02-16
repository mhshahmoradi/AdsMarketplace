namespace TelegramAds.Features.Campaigns.CreateCampaign;

public sealed record CreateCampaignRequest(
    string Title,
    string Brief,
    decimal BudgetInTon,
    int? MinSubscribers,
    int? MinAvgViews,
    string? TargetLanguages,
    DateTime? ScheduleStart,
    DateTime? ScheduleEnd
);
