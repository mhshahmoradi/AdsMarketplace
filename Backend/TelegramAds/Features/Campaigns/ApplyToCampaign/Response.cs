namespace TelegramAds.Features.Campaigns.ApplyToCampaign;

public sealed record ApplyToCampaignResponse(
    Guid ApplicationId,
    Guid CampaignId,
    Guid ChannelId,
    decimal ProposedPriceInTon,
    string Status
);
