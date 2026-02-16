namespace TelegramAds.Features.Deals.CancelDeal;

public sealed record CancelDealResponse(Guid DealId, string Status, string? RefundStatus);
