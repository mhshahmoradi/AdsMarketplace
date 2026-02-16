namespace TelegramAds.Features.Listings.SearchListings;

public sealed record SearchListingsRequest(
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinSubscribers,
    int? MinAvgViews,
    string? Language,
    bool OnlyMine = false,
    int Page = 1,
    int PageSize = 20
);
