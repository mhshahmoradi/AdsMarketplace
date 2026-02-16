namespace TelegramAds.Features.Deals.ListDeals;

public sealed record ListDealsRequest(
    string? Role,
    string? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20
);
