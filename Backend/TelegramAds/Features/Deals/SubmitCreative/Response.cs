namespace TelegramAds.Features.Deals.SubmitCreative;

public sealed record SubmitCreativeResponse(
    Guid DealId,
    string Status,
    string CreativeText,
    DateTime SubmittedAt
);
