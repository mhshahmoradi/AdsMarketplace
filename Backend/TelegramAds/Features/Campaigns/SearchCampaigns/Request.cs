namespace TelegramAds.Features.Campaigns.SearchCampaigns;

public sealed record SearchCampaignsRequest(
    string? SearchTerm = null,
    int? MinBudgetTon = null,
    int? MaxBudgetTon = null,
    string? TargetLanguage = null,
    bool OnlyMine = false,
    int Page = 1,
    int PageSize = 20
);
