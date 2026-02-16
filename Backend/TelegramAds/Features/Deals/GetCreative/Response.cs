namespace TelegramAds.Features.Deals.GetCreative;

public sealed record GetCreativeResponse(
    Guid DealId,
    string Status,
    string? CreativeText,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    string? RejectionReason
);
