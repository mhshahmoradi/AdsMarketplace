namespace TelegramAds.Features.Deals.CancelDeal;

public sealed record CancelDealRequest(Guid DealId, string? Reason);
