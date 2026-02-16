namespace TelegramAds.Features.Channels.GetChannel;

public sealed record GetChannelResponse(
    Guid Id,
    long TgChannelId,
    string Username,
    string Title,
    string? Description,
    string? PhotoUrl,
    string Status,
    ChannelStatsDto? Stats,
    List<string> Tags,
    DateTime CreatedAt
);

public sealed record ChannelStatsDto(
    int SubscriberCount,
    double AvgViewsPerPost,
    double AvgSharesPerPost,
    double AvgReactionsPerPost,
    double AvgViewsPerStory,
    double AvgSharesPerStory,
    double AvgReactionsPerStory,
    double EnabledNotificationsPercent,
    int FollowersCount,
    int FollowersPrevCount,
    string? PrimaryLanguage,
    string? LanguagesGraphJson,
    double PremiumSubscriberCount,
    DateTime StatsStartDate,
    DateTime StatsEndDate,
    DateTime FetchedAt
);
