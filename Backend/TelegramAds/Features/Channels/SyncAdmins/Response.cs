namespace TelegramAds.Features.Channels.SyncAdmins;

public sealed record SyncAdminsResponse(
    Guid ChannelId,
    int TotalAdmins,
    int AddedCount,
    int RemovedCount
);
