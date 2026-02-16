namespace TelegramAds.Features.Deals.CreateDeal;

public sealed record CreateDealResponse(
    Guid Id,
    Guid ListingId,
    Guid ChannelId,
    string Status,
    decimal InitialPriceInTon
);
