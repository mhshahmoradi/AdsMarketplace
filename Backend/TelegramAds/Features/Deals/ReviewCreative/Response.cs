namespace TelegramAds.Features.Deals.ReviewCreative;

public sealed record ReviewCreativeResponse(
    Guid DealId,
    string Status,
    string Decision,
    string? RejectionReason,
    DateTime ReviewedAt
);
