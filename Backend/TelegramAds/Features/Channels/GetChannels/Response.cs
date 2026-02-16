namespace TelegramAds.Features.Channels.GetChannels;

public sealed record GetChannelsResponse(List<ChannelItem> Items, int TotalCount);

public sealed record ChannelItem(
    Guid Id,
    long TgChannelId,
    string? Username,
    string Title,
    string? Description,
    string Status,
    int? SubscriberCount,
    double? AvgViewsPerPost,
    List<string> Tags,
    bool IsOwner,
    bool IsAdmin,
    DateTime CreatedAt
);
