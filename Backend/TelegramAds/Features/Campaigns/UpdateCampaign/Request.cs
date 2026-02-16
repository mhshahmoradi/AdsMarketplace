namespace TelegramAds.Features.Campaigns.UpdateCampaign;

public sealed record UpdateCampaignRequest(
    string? Title,
    string? Brief,
    decimal? BudgetInTon,
    int? MinSubscribers,
    int? MinAvgViews,
    string? TargetLanguages,
    DateTime? ScheduleStart,
    DateTime? ScheduleEnd
);
