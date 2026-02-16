namespace TelegramAds.Features.Channels.VerifyBotAdmin;

public sealed record VerifyBotAdminRequest(Guid ChannelId, long RealUserTgId);
