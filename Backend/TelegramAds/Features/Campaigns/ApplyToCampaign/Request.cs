namespace TelegramAds.Features.Campaigns.ApplyToCampaign;

public sealed record ApplyToCampaignRequest(
    Guid CampaignId,
    Guid ChannelId,
    decimal ProposedPriceInTon,
    string? Note
);
