using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.UpdateCampaign;

public sealed record UpdateCampaignResponse(
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
    DateTime UpdatedAt
);
