namespace TelegramAds.Features.Channels.AddChannel;

public sealed record AddChannelResponse(
    Guid ChannelId,
    long TgChannelId,
    string Username,
    string Title,
    string? Description,
    string Status,
    int SubscriberCount
);
