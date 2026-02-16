namespace TelegramAds.Features.Channels.VerifyBotAdmin;

public sealed record VerifyBotAdminResponse(
    Guid ChannelId,
    bool IsVerified,
    bool IsBotAdmin,
    bool IsUserAdmin,
    string Status,
    string? Message,
    double? PremiumSubscriberCount = null
);
