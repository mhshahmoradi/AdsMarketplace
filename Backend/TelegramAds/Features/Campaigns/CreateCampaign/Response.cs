namespace TelegramAds.Features.Campaigns.CreateCampaign;

public sealed record CreateCampaignResponse(
    Guid Id,
    string Title,
    decimal BudgetInTon,
    string Status
);
