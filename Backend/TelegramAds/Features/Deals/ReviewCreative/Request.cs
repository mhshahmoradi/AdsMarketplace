namespace TelegramAds.Features.Deals.ReviewCreative;

public sealed record ReviewCreativeRequest(
    string Decision,
    string? RejectionReason
);
