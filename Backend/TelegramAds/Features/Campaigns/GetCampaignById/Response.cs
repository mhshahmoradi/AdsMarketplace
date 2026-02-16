namespace TelegramAds.Features.Campaigns.GetCampaignById;

public sealed record GetCampaignByIdResponse(
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
    Guid AdvertiserUserId,
    string? AdvertiserUsername,
    bool IsMine,
    List<CampaignApplicationItem> Applications,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record CampaignApplicationItem(
    Guid Id,
    Guid ChannelId,
    string ChannelTitle,
    string? ChannelUsername,
    decimal ProposedPrice,
    string? Message,
    string Status,
    DateTime CreatedAt
);
